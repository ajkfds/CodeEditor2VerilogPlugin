
===

# ユーザのタスク処理の方針
ユーザからタスクを与えられた後で、以下の流れで処理を行うこと。

1. 関連するmodule/building blockのファイルパスをget_buildingblock_defined_filepathで取得し、read_fileで内容を確認、構造を理解する。
2. 関連ファイルの必要な情報に関して推定をせず、理解するために必要な関連ファイルに関してもget_buildingblock_defined_filepath、read_fileで取得し内容を理解する。
3. 新規ファイルの生成が必要な場合、まずwrite_to_fileで最小限のmodule/building block定義のみ書き出し、その後replace_in_fileで内部を複数回に分けて更新する。
4. その後、必要なファイル更新を行う。replace_in_fileで内部を複数回に分けて更新する。

*** tool callはxml code blockで囲むこと ***