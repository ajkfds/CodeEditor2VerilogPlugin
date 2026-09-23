# CodeEditor2VerilogPlugin

## Verilog autocomplete / hint 情報の生成仕組み (部分parse + CompletionContext)

### 概要

入力中の caret 位置に対する autocomplete 候補と hint popup 情報は、
`Verilog/CompletionContext.cs` (`pluginVerilog.Verilog.CompletionContext`) が
**caret 直前までのテキストを切り出した部分ドキュメントの再parse (partial parse)** によって生成する。

既存の完全parse (full parse) で使用しているのと同じ parse 関数群
(`Verilog/Items/ModuleInstantiation.cs`, `Verilog/BuildingBlocks/Module.cs` 等) を、
オプション引数 `CompletionContext? completionContext` を渡して再利用する。
parse 関数内の随所にある `completionContext != null && word.Eof` 分岐が、
「ユーザが今まさに入力しようとしている位置」に応じた hint / autocomplete 情報を
completionContext に追記する。

この設計により:

- 補完候補生成ロジックが文法 parse と単一ソースで共有される (BNF 定義に沿った parse コードを二重管理しない)
- parse 対象が「所属 region 先頭〜caret 直前」のみに限定され、UI 応答性が保たれる

### 全体フロー

```
ユーザ入力 (TextEntered)
  └─ CodeEditor2: CodeCompleteHandler.TextEntered()
       └─ ITextFile.GetAutoCompleteItems(caretOffset)
            └─ VerilogFile / VerilogModuleInstance / InterfaceInstance / ImportedPackage が override
                 └─ VerilogCommon.AutoComplete.GetAutoCompleteItems(item, parsedDocument, index)
                      └─ new Verilog.CompletionContext(item, parsedDocument, index)
                           ├─ GetAutoCompleteTarget() : CandidateWord / NameSpace / NamedElement の特定
                           ├─ parsedDocument.GetDocumentRegionAt() : caret を含む IDocumentRegeion の特定
                           └─ 部分parse : region 型に応じた Parse 関数を completionContext 付きで再実行
  └─ CodeCompleteHandler 側で表示
       ├─ CarletPopupItems  → input-time hint popup (caret 直下)
       └─ AutoCompleteItems → auto-complete dropdown (CandidateWord でフィルタ)
```

### CompletionContext コンストラクタの処理詳細

`Verilog/CompletionContext.cs` のコンストラクタは以下の順で処理する。

#### (1) 補完ターゲットの特定 (`GetAutoCompleteTarget`)

`VerilogCommon.AutoComplete.GetAutoCompleteTarget()` が caret 行の行頭〜caret までの
テキストを軽量に WordScanner で走査し、以下を決定する:

| 出力 | 意味 |
|---|---|
| `CandidateWord` | 現在入力中の識別子 (補完フィルタ対象文字列) |
| `CandidateStartIndex` | CandidateWord の開始位置 (ドキュメント絶対index) |
| `NameSpace` | caret 行頭位置での NameSpace (階層を持たせて `parsedDocument.GetNameSpace()` で取得) |
| `NamedElement` | `.` / `::` / `->` で繋がった参照 prefix が解決できた場合、その先の要素 (DataObject / NameSpace / instanced BuildingBlock 等) |

`NamedElement` が非nullの場合 (例: `inst0.` まで入力した状態) は、その要素の
sub-element が補完候補になる。nullの場合は NameSpace を上位階層へ遡って候補を集める。

さらにコメント中 (`//`, `/*` 直後) でも補完が働くよう、行テキストからコメント開始位置を
取り除いて CandidateStartIndex を補正している。caret 直前が空白/タブの場合は補完対象外。

#### (2) 部分parse対象 region (`IDocumentRegeion`) の特定

```
IndexReference iref = IndexReference.Create(lineStartIndex, parsedDocument);
iitem = parsedDocument.GetDocumentRegionAt(iref);
```

caret 行頭の index を含む parse済み region を `ParsedDocument.GetDocumentRegionAt()`
で取得する。region は `Verilog/Items/IDocumentRegeion.cs` の `IDocumentRegeion`
interface (`BeginIndexReference` / `LastIndexReference` を持つ) で表現される。

例えば caret が module instantiation 文の中にあれば `Verilog.Items.ModuleInstantiation`
が、module 本体の項目位置であれば `BuildingBlocks.Module` が得られる。
caret がどの region にも属さない場合、parse開始位置は NameSpace の
`BeginIndexReference.RootIndex` にフォールバックする。

#### (3) 部分parseの実行

```
string blockText = item.CodeDocument.CreateString(parseBlockIndex, CandidateStartIndex - parseBlockIndex);
```

「region 先頭 (`iitem.BeginIndexReference.RootIndex`) 〜 `CandidateStartIndex` (caret直前)」
の範囲文字列で新規 `pluginVerilog.CodeEditor.CodeDocument` を生成し、`WordScanner` を作成。
その後、region 型に応じて対応する parse 関数を **同期的に** 再実行する
(補完は UI 入力フローから呼ばれるため `GetAwaiter().GetResult()` で待機):

| `iitem` の型 | 実行される parse 関数 |
|---|---|
| `Verilog.Items.ModuleInstantiation` | `Verilog.Items.ModuleInstantiation.ParseAsync(word, NameSpace, this)` |
| `Module` | `Module.ParseCreateAsync(word, module.ParameterOverrides, module.Attribute, module.BuildingBlock, item, false, this)` |

parse 関数には `this` (completionContext) が渡され、以降の「EOF到達時の情報追記」が
有効化される。

### 部分parseと hint / autocomplete 情報の追記 (`word.Eof` パターン)

#### 基本原則

各 `IDocumentRegeion` の parse 関数内では、**入力途中のテキストが部分ドキュメントの
末尾に達したタイミング (== ユーザが今入力しようとしている位置)** を
`completionContext != null && word.Eof` で検出し、その位置に適した hint / autocomplete
情報を completionContext に追記して `return` する。

`WordScanner.Eof` は、clone された word pointer の階層 (stock) を含め、
部分ドキュメントをすべて走査し終えたことを示す。
部分parseではドキュメントが「caret 直前」で切れているため、
通常の完全parseでは文法エラーになるような「途切れた入力」でも、
`word.Eof` が「ユーザの入力位置に到達した」ことを意味する正常終了として扱える。

補完候補自体は parse 関数ではなく completionContext 側の append 系メソッドが生成する
(`AppendExpression`, `AppendKeywords`, `AppendAll`, `AppendModuleInstanceSnippets` 等)。
parse 関数は「どの文法位置にいるか」を知っているため、それを呼び分ける役割を担う。

#### 例: `Verilog/Items/ModuleInstantiation.cs`

module instantiation の named port connection
(`.port_name ( ... )`) のparse中、caret が括弧の中に達した場合:

```
if (completionContext != null && word.Eof)
{
    Port? port = instancedModule?.Ports[pinName];
    if (port != null) completionContext.CarletPopupItems.Add(
        new CodeEditor2.CodeEditor.PopupHint.PopupItem(port.GetLabel()));
    return;
}
```

- 接続中の port 名 (`pinName`) から instanced module の `Port` を引き当て
- port の方向/型/ビット幅ラベル (`port.GetLabel()`) を `CarletPopupItems` に追加
- これにより `.clk ( ` まで入力した時点で、接続すべき port の型情報が caret 直下に hint popup 表示される

この分岐は括弧開き直後と expression parse 後の2箇所にあり、
outPort (`output` / `inout`) の場合は `Expression.ParseCreateVariableLValue(..., completionContext)`
に、それ以外は `ParseCreateAcceptImplicitNet(...)` 系に completionContext が伝播する。
Expression parse 系 (`Verilog/Expressions/Expression.cs` → `Primary.ParseCreate` 等) も
completionContext を引数に持ち、文法階層を辿りながら候補生成を委譲する。

さらに `ParseAsync` の冒頭では

```
if (completionContext != null)
{
    completionContext.AppendExpression();
}
```

により、module identifier 位置 (module名を入力中) での補完対象を
DataObject / Function / NameSpace に絞った候補を先に投入する。

#### 例: `Verilog/BuildingBlocks/Module.cs`

module 本体の item 列をparse中、`word.Eof` (＝ module 閉じ前・入力位置が行末尾に達した) で
`completionContext` が非nullなら、行頭判定 (`onLineStart`) を踏まえた候補に差し替える:

```
if (word.Eof)
{
    if (completionContext != null)
    {
        completionContext.AutoCompleteItems.Clear();
        completionContext.AppendKeywords(new List<string> {
            "endmodule", "always", "assign", "initial",
            "bit","logic","reg","byte", ... "genvar"
        });
        completionContext.AppendModuleInstanceSnippets((ac) => true);
        return;
    }
    break;
}
```

- 新しい statement を書き始める位置として、module item の先頭になり得る keyword 群を限定
- `AppendModuleInstanceSnippets()` は行頭かつ `Module`/`GenerateBlock` 内という条件で、
  project 全体から Module 定義を列挙し、instance 文の snippet を候補に追加
  (snippet の選択は `CandidateWord.Length > 1` などの条件で絞られる)

### CompletionContext の候補生成メソッド

parse 関数から呼び出される append 系メソッドの役割:

| メソッド | 生成する候補 | 主な filter 条件 |
|---|---|---|
| `AppendAll()` | NamedElements 全部 + keyword 全部 + system function/task | 常にtrue |
| `AppendExpression()` | DataObject / Function / NameSpace + system function | 式位置向け |
| `AppendKeywords(List<string>)` | 指定 keyword のみ | keywords.Contains(Text) |
| `AppendModuleInstanceSnippets()` | module instance 文 snippet | 行頭 + Module/GenerateBlock 内 + CandidateWord長>1 |

共通の下請け:

- `appendNamedElements(filter)`: `NamedElement` が非nullならその sub-element 列挙、
  nullなら `appendItemsUpward()` で NameSpace を親へ遡って列挙
  (`\0` 始まりの無名要素は除外、重複名は初出優先)
- `appendKeyword(filter)`: `AutoCompleteKeyword.AppendKeywordAutoCompleteItems()` が
  Verilog/SystemVerilog の keyword テーブルから生成
- `appendSystemFunction` / `appendSystemTask`: `CandidateWord` が `$` 始まりの場合に
  `ProjectProperty.SystemFunctions` / `SystemTaskParsers` の登録名から生成

生成される各候補は `Data.VerilogCommon.AutoCompleteItem`
(`CodeEditor2.CodeEditor.CodeComplete.AutocompleteItem` の派生) で、
色 (`CodeDrawStyle.ColorIndex`) / アイコン / `CompleteType`
(DataObject / Function / NameSpace / Task / Keyword) を持つ。

### CodeEditor2 側での表示振り分け

`CodeCompleteHandler.TextEntered()` (CodeEditor2/CodeEditor/CodeComplete/CodeCompleteHandler.cs) が
`ITextFile.GetAutoCompleteItems(caretOffset)` の結果を消費する:

- `CarletPopupItems` が空でなければ `Controller.CodeEditor.OpenPopup()` で
  caret 直下の hint popup を表示 (auto-complete dropdown が動作中 `working == true` の場合は
  衝突防止のため表示しない)
- `AutoCompleteItems` は `CandidateWord` で前方一致フィルタされ、
  auto-complete dropdown として表示・更新される

### 対比: mouse-over hint は部分parseを使わない

同じ hint 情報系でも mouse-over popup は異なる経路を取る:

```
PointerMoved → PopupHandler → ITextFile.GetPopupItem(version, index)
  → VerilogCommon.AutoComplete.GetPopupItem()
    → parsedDocument.GetPopupItem(index, text)
```

`ParsedDocument.GetPopupItem()` は **既存の完全parse結果** (Messages, NameSpace,
NamedElements, Macros) を参照する軽量 lookup であり、部分parseを実行しない。
入力時にしか存在しない「途中入力の文法位置」を解釈する必要がある autocomplete /
input-time hint が部分parseを要求する一方、mouse-over は常に確定済みテキスト上の
位置であるため parse済み構造の参照で足りる、という使い分けになっている。

### 注意点・既知の制約

- 部分parseは `GetAwaiter().GetResult()` による同期待機を伴うため、UI スレッド上で
  呼ばれることを前提にしている。region が巨大な場合 (例: 数千行の module 本体の先頭に
  caret がある場合) は部分parseのコストもregion全体parseには及ばないものの無視できない
- `ParseMode == LoadParse` の場合、parse関数内部では早期リターンするため
  completionContext 追記は実質的に EditParse 相当の経路でのみ有効
- 部分parseで生成される要素 (ModuleInstantiation 等) は `nameSpace.NamedElements.Add` /
  `nameSpace.DocumentRegions.Add` を通常parseと同じコードパスで通る
  (`word.Prototype` は false で走るため実登録)。`GetAutoCompleteTarget` が返す
  NameSpace は `parsedDocument.GetNameSpace()` → `GetHierarchyNameSpace()` が返す
  **既存 parse 済み階層の実インスタンス**であるため、部分parseで生成した要素
  (一時ドキュメントの index 空間を持つ IndexReference を含む) が
  既存 ParsedDocument 側の NamedElements / DocumentRegions に一時的に登録されうる
  (コード読解に基づく挙動。次回の再parseで置き換えられる前提の設計と思われる)

---

## 修正案: function call 引数入力中の hint / autocomplete 表示の追加

### 現状の課題 (コード解析結果)

module instantiation の named port connection では `.clk (` まで入力すると port 情報の
hint popup が表示されるが、**function call / let call の引数入力中は同様の hint が
一切表示されない**。原因は以下の通り (`Verilog/Expressions/ListOfArguments.cs`,
`Verilog/Expressions/FunctionCall.cs` の実装確認に基づく):

1. **`ListOfArguments.ParseListOfArguments` が `completionContext` を使用していない**
   - 引数で `CompletionContext? completionContext = null` を受けているが、
     関数内で一度も参照していない (`word.Eof` 分岐の実装が存在しない)
   - `word.Eof` パターン (部分parse末尾 = 入力位置) の検出が行われないため、
     `func(arg1, ` のような入力中に何も hint が出ない

2. **`completionContext` の式 parse への伝播漏れ**
   - positional argument: `Expression.ParseCreate(word, usedNameSpace)` に
     `completionContext` が渡されていない
   - named argument: `Expression.ParseCreate(word, (NameSpace)portNameSpace)` も同様に未伝播
   - このため `func(arg1, arg2|` のように引数の式入力中も内側の式位置 hint が出ない

3. **function 名入力位置での候補生成の欠落**
   - `ModuleInstantiation.ParseAsync` は冒頭で `completionContext.AppendExpression()` を
     呼んで module 名入力位置の候補 (DataObject / Function / NameSpace) を生成するが、
     `FunctionCall.ParseCreate` には同等の処理がない

4. **対象外経路での同種の欠落**
   - `ModuleInstantiation.parseOrderedPortConnections` (ordered port connection) は
     `completionContext` を引数に取らない。named のみ hint が効いており
     `inst0(clk, |` のような ordered 接続入力中は port 情報が出ない
   - `BuiltinMethodCall.ParseCreate` (`data.find(...)` 等) も `completionContext` を
     受け取らず、引数ループ内の `Expression.ParseCreate` に伝播しない
   - `Task_` 呼び出し (task call) の引数経路も同様の確認・対応が望ましい

### 追加すべき機能

`ModuleInstantiation` の named port connection と同一の `word.Eof` パターンで、
function call の文法位置に応じた hint / autocomplete を `CompletionContext` に追記する。

#### 機能1: 引数位置での port (argument) 情報 hint (`CarletPopupItems`)

`foo(arg1, ` のカンマ直後・`foo(` の括弧直後で、**次に入力すべき引数の宣言ラベル**
(`input logic [7:0] data` 形式 = `Port.GetLabel()`) を hint popup 表示する。

実装方針 (`Verilog/Expressions/ListOfArguments.cs`):

```csharp
public static void ParseListOfArguments(WordScanner word, NameSpace usedNameSpace,
    IPortNameSpace? portNameSpace,
    Dictionary<string, Expressions.Expression> portConnection,
    out bool constantConnected,
    CompletionContext? completionContext = null
    )
{
    ...
    word.MoveNext();

    // (a) 括弧開き直後の EOF: 最初の引数 (or 引数なし) の hint
    if (completionContext != null && word.Eof)
    {
        appendArgumentPopupItems(completionContext, portNameSpace, 0);
        return;
    }

    int i = 0;
    ...
    while (!word.Eof)
    {
        ...
        if (word.Text == ",")
        {
            // (b) カンマ直後の EOF: 次の引数 port の hint
            if (completionContext != null && word.Eof)
            {
                appendArgumentPopupItems(completionContext, portNameSpace, i + 1);
                return;
            }
            ...
        }

        // (c) 引数の式 parse へ completionContext を伝播
        Expression? expression = Expression.ParseCreate(word, usedNameSpace, completionContext);
        ...
    }
}

// (d) helper: 指定 index の port ラベルを CarletPopupItems に追加
private static void appendArgumentPopupItems(
    CompletionContext completionContext, IPortNameSpace? portNameSpace, int index)
{
    if (portNameSpace == null) return;
    if (index >= portNameSpace.PortsList.Count) return;   // 全引数済み
    DataObjects.Port port = portNameSpace.PortsList[index];
    completionContext.CarletPopupItems.Add(
        new CodeEditor2.CodeEditor.PopupHint.PopupItem(port.GetLabel()));
}
```

- positional 引数は `portNameSpace.PortsList[i]` で次の引数 port が一意に決まるため、
  `Port.GetLabel()` (direction / 型 / ビット幅 / port 名 / コメント) をそのまま hint に使える
- `DefaultArgument` を持つ引数ではラベルに `[= default]` を併記すると省略可能であることが伝わる
- 全引数入力済み (`index >= PortsList.Count`) の場合は追加 hint なしで return

#### 機能2: named argument (`.name(expr)`) の補完

`tf_call ::= ... [ "(" list_of_arguments ")" ]` の named 形式
(`list_of_arguments ::= ... { , "." identifier ( [ expression ] ) }`) に対して:

- **`.` 直後の EOF**: 未接続の引数名一覧を autocomplete 候補に追加。
  positional で接続済みの引数は除外する (実装済み `connectedPorts` HashSet を流用)。
  `AppendExpression` 系の DataObject 候補ではなく、
  `portNameSpace.PortsList` から引数名列挙の候補を生成する
- **`.name(` 直後の EOF**: その引数の `Port.GetLabel()` を `CarletPopupItems` に追加
- **`.name(expr` の EOF**: `Expression.ParseCreate` への伝播により式位置 hint を出す
  (named 引数は式を `(NameSpace)portNameSpace` の名前空間で parse している点に注意。
  completionContext 側の候補生成は `NameSpace`/`NamedElement` ベースなので、
  伝播する `NameSpace` は `usedNameSpace` とどちらが適切か要検証
  ―― 引数名スコープの解決位置は port 定義側だが、入力補完対象は呼び出し側スコープ)

#### 機能3: function 名入力位置での候補生成

`FunctionCall.ParseCreate` の冒頭 (または呼び出し元 `Primary.parseCreate` の
function call 判定直前) で `AppendExpression()` を呼ぶ。
`ModuleInstantiation.ParseAsync` 冒頭と同じパターンで、
`func|` のように名前入力中に DataObject / Function / NameSpace / system function
の候補を表示する。なお `AppendExpression` は既存の
`GetAutoCompleteTarget` の結果 (`NameSpace`/`NamedElement`) を使うため、
`FunctionCall.ParseCreate` 内ではなく **`CompletionContext` コンストラクタの
部分parse開始位置** (式が切り出される前) で効かせる設計になる点に注意。
現状は `Module` 本体の `word.Eof` (行頭) でしか keyword 候補生成が
働いておらず、文途中の function 名位置では `ModuleInstantiation.ParseAsync` 経由
(ModuleInstantiation region 内) でのみ効く。function call が statement 途中に
埋め込まれるケース (`assign x = func(|`) では部分parseの region が
Module 全体になり `Module.ParseCreateAsync` が走るため、
その statement parse 経路で `Expression.ParseCreate(..., completionContext)` が
繋がっていれば機能3は自然に成立する (要: statement 系 parse の伝播確認)。

#### 機能4: 横展開 (同種経路への展開)

| 対象 | 現状 | 追加内容 |
|---|---|---|
| `ModuleInstantiation.parseOrderedPortConnections` | `completionContext` 未対応 | 引数に `completionContext` を追加し、`instancedModule.PortsList[i]` の `GetLabel()` を `word.Eof` 時に `CarletPopupItems` へ。`Expression.ParseCreate` にも伝播 |
| `BuiltinMethodCall.ParseCreate` | `completionContext` 未対応 | 引数に `completionContext` を追加し、`method.PortsList[i]` の `GetLabel()` を hint 表示、`Expression.ParseCreate` に伝播 |
| task call (`Task_` 呼び出し) | 未確認 | `IPortNameSpace` を実装するため `ListOfArguments` 共通化で機能1/2がそのまま効くはず。parse 経路の completionContext 伝播のみ確認が必要 |
| `Class` constructor 呼び出し (`Class.cs` 内) | `ParseListOfArguments` 呼び出しで `completionContext` 非渡し | `completionContext` を渡すのみ |

### 設計上の注意

- **`word.Eof` パターンとの整合**: 既存設計 (README 上部セクション参照) では
  「部分parse末尾到達 = 入力位置」を `completionContext != null && word.Eof` で検出する。
  function call 引数内でも同一パターンを守ることで、
  parse 関数が「どの文法位置にいるか」を知り、候補生成を completionContext に委譲する
  単一ソース設計が維持される
- **`word.Eof` での早期 return 時の副作用**: `ModuleInstantiation.parseNamedPortConnection`
  の既存実装と同様、EOF 検出したらそれ以上の文法検証 (bitwidth mismatch 等) を
  行わず return すること。部分parseで生成された Expression は
  一時ドキュメントの index 空間を持つため、参照登録を含む後続処理を走らせない方が安全
  (既存の named port connection 分岐と同じ規律)
- **`out bool constantConnected` の扱い**: EOF 早期 return 時は
  `constantConnected = true` (初期値) のまま返す。呼び出し元の
  `FunctionCall.Constant` 計算に影響するが、部分parseの結果は
  一時オブジェクトにのみ反映されるため実害はないはず (要確認)
- **UI 表示の既存インフラ**: `CarletPopupItems` → `CodeCompleteHandler.TextEntered` →
  `Controller.CodeEditor.OpenPopup()` (caret 直下 hint popup) の表示経路は
  実装済み (メインリポジトリ CodeEditor2 側)。plugin 側は
  `CarletPopupItems` に `PopupItem` を追加するだけで表示される
- **検証方法**: 部分parseは `EditParse` モード時のみ実効的に動作するため
  (README 注意点参照)、実際の入力シナリオで
  `func(` / `func(a, ` / `func(.p|` / `func(.p(` の各位置で
  hint popup・autocomplete dropdown の出ることを確認する

### 期待される効果

- module instantiation と同等の入力支援が function / let / builtin method / task 呼び出しで得られる
- 引数の方向・型・ビット幅が入力中に判明するため、bitwidth mismatch を
  書いた後に warning で知るのではなく、入力前に防げる
- named argument の補完により、長い引数リストを持つ function の
  引数名タイプミス (実行時エラーではなく parse エラーとして検出される現状の
  `undefined port` エラー) を未然に防げる