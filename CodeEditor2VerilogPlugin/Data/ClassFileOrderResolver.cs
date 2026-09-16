using pluginVerilog.Verilog;
using pluginVerilog.Verilog.BuildingBlocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace pluginVerilog.Data
{
    /// <summary>
    /// SystemVerilog のクラスファイル間の依存関係を解析し、
    /// シミュレーション時のコンパイル順 (setup.Files) を整列するヘルパー。
    ///
    /// 制約:
    ///   1. 参照前の宣言が必要
    ///   2. extends 関係はベースクラスが先
    ///   3. implements 関係はインターフェースクラスが先
    ///   4. 循環参照 (相互参照) は typedef class で前方宣言
    ///   5. 同一コンパイルユニット (ファイル) 内では記述順がそのまま依存関係に影響
    /// </summary>
    public static class ClassFileOrderResolver
    {
        public class Result
        {
            /// <summary>
            /// 整列後の setup.Files 相当のファイルリスト。
            /// 循環 (SCC) 内部ではトポロジカルな順序に並べ替えず、
            /// 元の順序を保持して前方宣言 (typedef class) で吸収する。
            /// </summary>
            public List<IVerilogRelatedFile> OrderedFiles { get; }

            /// <summary>
            /// ファイル毎に必要な typedef class 前方宣言。
            /// 各ファイルがコンパイルされるとき、ファイル先頭に挿入すべき
            /// "typedef class X;" 行のソース。
            /// key: IVerilogRelatedFile (前方宣言が必要なファイル)
            /// value: ファイル先頭に前置する typedef class 行 (改行区切り)
            /// </summary>
            public Dictionary<IVerilogRelatedFile, string> ForwardDeclarations { get; }

            /// <summary>
            /// 循環参照で typedef class を発行したグループの情報。
            /// ログ / 警告用。
            /// </summary>
            public List<HashSet<IVerilogRelatedFile>> CyclicGroups { get; }

            public Result(
                List<IVerilogRelatedFile> orderedFiles,
                Dictionary<IVerilogRelatedFile, string> forwardDeclarations,
                List<HashSet<IVerilogRelatedFile>> cyclicGroups)
            {
                OrderedFiles = orderedFiles;
                ForwardDeclarations = forwardDeclarations;
                CyclicGroups = cyclicGroups;
            }
        }

        /// <summary>
        /// setup.Files の順序を、クラス相互の依存関係 (extends / implements / 参照) に基づいて整列する。
        /// </summary>
        /// <param name="files">現在の setup.Files</param>
        /// <param name="setup">SimulationSetup (参照元 Project / ProjectProperty 解決用)</param>
        /// <returns>整列後のファイルリスト、循環参照の前方宣言、循環グループのサマリ</returns>
        public static Result ResolveOrder(List<IVerilogRelatedFile> files, SimulationSetup setup)
        {
            if (files == null) files = new List<IVerilogRelatedFile>();
            var ordered = new List<IVerilogRelatedFile>(files);
            var forwardDeclarations = new Dictionary<IVerilogRelatedFile, string>();
            var cyclicGroups = new List<HashSet<IVerilogRelatedFile>>();

            // 1. ファイル⇔クラス名の対応を構築
            var fileToDefinedClasses = BuildDefinedClassMap(ordered, setup);
            var classToFile = new Dictionary<string, IVerilogRelatedFile>(StringComparer.Ordinal);
            foreach (var kv in fileToDefinedClasses)
            {
                foreach (var cls in kv.Value)
                {
                    if (!classToFile.ContainsKey(cls))
                    {
                        classToFile[cls] = kv.Key;
                    }
                }
            }

            // 2. ファイル間の依存関係エッジを構築
            //    file B が file A のクラスを参照 / extends / implements するとき、A → B
            //    (A を先にコンパイル)
            var edges = new Dictionary<IVerilogRelatedFile, HashSet<IVerilogRelatedFile>>();
            foreach (var f in ordered)
            {
                edges[f] = new HashSet<IVerilogRelatedFile>();
            }

            // 2a. 参照 (ReferencedUnitNameSpace)
            AddReferenceEdges(ordered, fileToDefinedClasses, classToFile, edges);

            // 2b. extends / implements
            AddExtendsImplementsEdges(ordered, fileToDefinedClasses, classToFile, edges);

            // 3. 強連結成分 (SCC) を分解し、SCC 内は元順序を維持、
            //    SCC 間はトポロジカル順に整列する (Kahn's algorithm)。
            var sccs = TarjanSCC(ordered, edges);
            var sccIndex = new Dictionary<IVerilogRelatedFile, int>();
            for (int i = 0; i < sccs.Count; i++)
            {
                foreach (var f in sccs[i]) sccIndex[f] = i;
            }

            // SCC 間エッジを構築
            var sccEdges = new HashSet<(int from, int to)>[sccs.Count];
            var sccInDegree = new int[sccs.Count];
            for (int i = 0; i < sccs.Count; i++) sccEdges[i] = new HashSet<(int, int)>();
            foreach (var fromKv in edges)
            {
                int fromScc = sccIndex[fromKv.Key];
                foreach (var to in fromKv.Value)
                {
                    int toScc = sccIndex[to];
                    if (fromScc == toScc) continue;
                    if (sccEdges[fromScc].Add((fromScc, toScc)))
                    {
                        sccInDegree[toScc]++;
                    }
                }
            }

            // Kahn's algorithm: indegree 0 の SCC から取り出す。
            // 安定化のため、元順序での出現位置が早い SCC を優先。
            var sccOrderIndex = new Dictionary<IVerilogRelatedFile, int>();
            for (int i = 0; i < ordered.Count; i++) sccOrderIndex[ordered[i]] = i;

            var ready = new SortedSet<int>(Comparer<int>.Create(
                (a, b) =>
                {
                    int firstA = sccs[a].Min(f => sccOrderIndex[f]);
                    int firstB = sccs[b].Min(f => sccOrderIndex[f]);
                    return firstA.CompareTo(firstB);
                }));

            for (int i = 0; i < sccs.Count; i++) if (sccInDegree[i] == 0) ready.Add(i);

            var sccOrdered = new List<int>();
            while (ready.Count > 0)
            {
                int top = ready.Min;
                ready.Remove(top);
                sccOrdered.Add(top);
                foreach (var (_, to) in sccEdges[top])
                {
                    sccInDegree[to]--;
                    if (sccInDegree[to] == 0) ready.Add(to);
                }
            }

            // 循環が残った場合は append (理論上はTarjanで全SCCを取っているので発生しないはず)
            for (int i = 0; i < sccs.Count; i++) if (!sccOrdered.Contains(i)) sccOrdered.Add(i);

            // 4. SCC 単位での順序を構築
            var result = new List<IVerilogRelatedFile>();
            foreach (int sccId in sccOrdered)
            {
                var members = new List<IVerilogRelatedFile>(sccs[sccId]);
                members.Sort((a, b) => sccOrderIndex[a].CompareTo(sccOrderIndex[b]));
                result.AddRange(members);
            }

            // 5. SCC が size > 1 の場合は循環グループとして記録し、
            //    各ファイルに typedef class 前方宣言を生成する。
            for (int i = 0; i < sccs.Count; i++)
            {
                if (sccs[i].Count <= 1) continue;

                var groupSet = new HashSet<IVerilogRelatedFile>(sccs[i]);
                cyclicGroups.Add(groupSet);

                // グループ内の各ファイル A について、
                // A が参照 / extends / implements している「グループ内の他ファイル B のクラス」を
                // typedef class で前置する。
                // グループ内の順序: 元順序で先に現れるファイルを typedef class しない。
                var orderedMembers = sccs[i].OrderBy(f => sccOrderIndex[f]).ToList();
                for (int j = 0; j < orderedMembers.Count; j++)
                {
                    var self = orderedMembers[j];
                    var selfClasses = fileToDefinedClasses.TryGetValue(self, out var sc) ? sc : new HashSet<string>(StringComparer.Ordinal);

                    var sb = new StringBuilder();
                    foreach (var other in orderedMembers)
                    {
                        if (other.Equals(self)) continue;
                        var otherClasses = fileToDefinedClasses.TryGetValue(other, out var oc) ? oc : new HashSet<string>(StringComparer.Ordinal);
                        // self が他のファイルを参照 / extends / implements している場合のみ typedef を発行する
                        if (!FileDependsOn(self, other, edges)) continue;
                        foreach (var cls in otherClasses)
                        {
                            // self 自身がこのクラス名を定義しているなら不要
                            if (selfClasses.Contains(cls)) continue;
                            sb.Append("typedef class ");
                            sb.Append(cls);
                            sb.Append(";\n");
                        }
                    }

                    string text = sb.ToString();
                    if (text.Length > 0)
                    {
                        // 既に同じファイルに対する typedef があれば追記
                        if (forwardDeclarations.TryGetValue(self, out var existing))
                        {
                            forwardDeclarations[self] = existing + text;
                        }
                        else
                        {
                            forwardDeclarations[self] = text;
                        }
                    }
                }
            }

            return new Result(result, forwardDeclarations, cyclicGroups);
        }

        /// <summary>
        /// 各ファイルで定義されているコンパイル単位 (UnitNameSpace) のクラス名を集める。
        /// </summary>
        private static Dictionary<IVerilogRelatedFile, HashSet<string>> BuildDefinedClassMap(
            List<IVerilogRelatedFile> files,
            SimulationSetup setup)
        {
            var map = new Dictionary<IVerilogRelatedFile, HashSet<string>>();
            foreach (var f in files)
            {
                map[f] = CollectClassNamesDefinedInFile(f, setup);
            }
            return map;
        }

        private static HashSet<string> CollectClassNamesDefinedInFile(IVerilogRelatedFile file, SimulationSetup setup)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);

            if (file == null) return result;

            // IVerilogRelatedFile から ProjectProperty / ParsedDocument を取得する。
            ProjectProperty? pp = null;
            ParsedDocument? pd = null;

            try
            {
                pp = file.ProjectProperty;
            }
            catch { /* ignore */ }

            try
            {
                pd = file.VerilogParsedDocument;
            }
            catch { /* ignore */ }

            if (pd?.Root != null)
            {
                foreach (var kv in pd.Root.BuildingBlocks)
                {
                    if (kv.Value is Class || kv.Value is InterfaceClass)
                    {
                        result.Add(kv.Value.Name);
                    }
                }
            }

            // Fallback: ProjectProperty.UnitNameSpace から name → file 逆引きで埋める。
            // (ParsedDocument が未ロード、または UnitNameSpace 経由の登録のみの場合)
            if (pp != null)
            {
                try
                {
                    foreach (var name in pp.UnitNameSpace.GetNameList(e => e is Class || e is InterfaceClass))
                    {
                        var src = pp.UnitNameSpace.GetFile(name);
                        if (src != null && src.Equals(file))
                        {
                            result.Add(name);
                        }
                    }
                }
                catch { /* ignore */ }
            }

            return result;
        }

        private static void AddReferenceEdges(
            List<IVerilogRelatedFile> files,
            Dictionary<IVerilogRelatedFile, HashSet<string>> fileToDefinedClasses,
            Dictionary<string, IVerilogRelatedFile> classToFile,
            Dictionary<IVerilogRelatedFile, HashSet<IVerilogRelatedFile>> edges)
        {
            foreach (var file in files)
            {
                ParsedDocument? pd;
                try { pd = file.VerilogParsedDocument; } catch { pd = null; }
                if (pd == null) continue;

                var referencedNames = pd.ReferencedUnitNameSpace;
                if (referencedNames == null) continue;

                foreach (var name in referencedNames)
                {
                    if (!classToFile.TryGetValue(name, out var dep)) continue;
                    if (dep.Equals(file)) continue;

                    // dep → file (dep を先にコンパイル)
                    if (edges.TryGetValue(dep, out var set))
                    {
                        set.Add(file);
                    }
                }
            }
        }

        private static void AddExtendsImplementsEdges(
            List<IVerilogRelatedFile> files,
            Dictionary<IVerilogRelatedFile, HashSet<string>> fileToDefinedClasses,
            Dictionary<string, IVerilogRelatedFile> classToFile,
            Dictionary<IVerilogRelatedFile, HashSet<IVerilogRelatedFile>> edges)
        {
            foreach (var file in files)
            {
                ParsedDocument? pd;
                try { pd = file.VerilogParsedDocument; } catch { pd = null; }
                if (pd?.Root == null) continue;

                foreach (var bb in pd.Root.BuildingBlocks.Values)
                {
                    if (bb is Class cls)
                    {
                        if (cls.ExtendedClass != null)
                        {
                            var baseName = cls.ExtendedClass.Name;
                            if (classToFile.TryGetValue(baseName, out var dep) && !dep.Equals(file))
                            {
                                if (edges.TryGetValue(dep, out var set)) set.Add(file);
                            }
                        }
                        foreach (var iface in cls.ImplementedInterfaceClasses)
                        {
                            if (iface == null) continue;
                            var ifName = iface.Name;
                            if (classToFile.TryGetValue(ifName, out var dep) && !dep.Equals(file))
                            {
                                if (edges.TryGetValue(dep, out var set)) set.Add(file);
                            }
                        }
                    }
                    else if (bb is InterfaceClass icls)
                    {
                        foreach (var baseIface in icls.ExtendedInterfaceClasses)
                        {
                            if (baseIface == null) continue;
                            var baseName = baseIface.Name;
                            if (classToFile.TryGetValue(baseName, out var dep) && !dep.Equals(file))
                            {
                                if (edges.TryGetValue(dep, out var set)) set.Add(file);
                            }
                        }
                    }
                }
            }
        }

        private static bool FileDependsOn(IVerilogRelatedFile self, IVerilogRelatedFile other, Dictionary<IVerilogRelatedFile, HashSet<IVerilogRelatedFile>> edges)
        {
            return edges.TryGetValue(other, out var set) && set.Contains(self);
        }

        // --- Tarjan's SCC ---
        private static List<HashSet<IVerilogRelatedFile>> TarjanSCC(
            List<IVerilogRelatedFile> nodes,
            Dictionary<IVerilogRelatedFile, HashSet<IVerilogRelatedFile>> edges)
        {
            var indices = new Dictionary<IVerilogRelatedFile, int>();
            var lowlinks = new Dictionary<IVerilogRelatedFile, int>();
            var onStack = new HashSet<IVerilogRelatedFile>();
            var stack = new Stack<IVerilogRelatedFile>();
            var sccs = new List<HashSet<IVerilogRelatedFile>>();
            int index = 0;

            // StrongConnect の非再帰実装
            void StrongConnect(IVerilogRelatedFile v)
            {
                var workStack = new Stack<(IVerilogRelatedFile v, IEnumerator<IVerilogRelatedFile> iter)>();
                indices[v] = index;
                lowlinks[v] = index;
                index++;
                stack.Push(v);
                onStack.Add(v);
                workStack.Push((v, edges[v].GetEnumerator()));

                while (workStack.Count > 0)
                {
                    var (current, iter) = workStack.Peek();
                    if (iter.MoveNext())
                    {
                        var w = iter.Current;
                        if (!indices.ContainsKey(w))
                        {
                            indices[w] = index;
                            lowlinks[w] = index;
                            index++;
                            stack.Push(w);
                            onStack.Add(w);
                            workStack.Push((w, edges[w].GetEnumerator()));
                        }
                        else if (onStack.Contains(w))
                        {
                            lowlinks[current] = Math.Min(lowlinks[current], indices[w]);
                        }
                    }
                    else
                    {
                        // Done with successors of v
                        if (lowlinks[current] == indices[current])
                        {
                            var scc = new HashSet<IVerilogRelatedFile>();
                            IVerilogRelatedFile w;
                            do
                            {
                                w = stack.Pop();
                                onStack.Remove(w);
                                scc.Add(w);
                            } while (!w.Equals(current));
                            sccs.Add(scc);
                        }
                        workStack.Pop();
                        if (workStack.Count > 0)
                        {
                            var parent = workStack.Peek().v;
                            lowlinks[parent] = Math.Min(lowlinks[parent], lowlinks[current]);
                        }
                    }
                }
            }

            foreach (var v in nodes)
            {
                if (!indices.ContainsKey(v)) StrongConnect(v);
            }

            return sccs;
        }
    }
}
