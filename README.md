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