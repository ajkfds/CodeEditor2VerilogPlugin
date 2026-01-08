# Role
あなたは熟練のLSI設計・検証エンジニアです。
提供された下位モジュールの設計資産（Markdown/RTL）を深く理解した上で、それらを統合する上位モジュールの仕様書を、正確かつ技術的に深い洞察を持って作成してください。

# Constraints
- 下位モジュールの詳細を単に羅列するのではなく、上位レイヤでの「統合ロジック」と「データフロー」に焦点を当てること。
- 接続関係は必ず Mermaid記法の `graph LR` を用いて視覚化すること。
- 専門用語（DisplayPortのパケット名やAHBの信号名など）は正確に使用すること。

# Context: Reference Information (Sub-modules)
下位モジュールの仕様と実装です。これらは上位モジュールの構成要素となります。

<sub_module_group>
  <sub_module name="[モジュールAの名前]">
    <spec_markdown>
    [ここにモジュールAの修正済みMarkdownを貼り付け]
    </spec_markdown>
    <rtl_code>
    [ここにモジュールAのRTLコードを貼り付け]
    </rtl_code>
  </sub_module>

  <sub_module name="[モジュールBの名前]">
    <spec_markdown>
    [ここにモジュールBの修正済みMarkdownを貼り付け]
    </spec_markdown>
    <rtl_code>
    [ここにモジュールBのRTLコードを貼り付け]
    </rtl_code>
  </sub_module>
</sub_module_group>

# Context: Target RTL (The Main Subject)
今回ドキュメントを作成する対象の最上位（または上位）RTLです。

<target_rtl>
[ここに上位モジュールのRTLコードを貼り付け]
</target_rtl>

# Output Format
以下の構成でMarkdown形式のドキュメントを出力してください。

1. **モジュール概要**: このモジュールがシステム全体で果たす役割。
2. **内部ブロック接続図 (Mermaid)**: `graph LR` を使用。下位モジュール間の主要なパスとプロトコル名を明記。
3. **主要機能とデータフロー**: 上位レイヤでどのようにパケットや信号が処理・変換されるか。
4. **レジスタ/メモリマップ（存在する場合）**: アドレス、フィールド名、アクセス属性、機能説明。
5. **インターフェース一覧**: 外部接続ポートの定義と振る舞い。
6. **設計上の特記事項**: タイミング制約やエラー処理、特殊なステート遷移など。

---
上記ガイドラインに従い、[Parent_Module_Name] の仕様書を作成してください。