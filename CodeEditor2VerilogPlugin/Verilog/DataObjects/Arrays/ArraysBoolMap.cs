using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    /// <summary>
    /// boolの多次元array object, 多次元のbit配列を小さいメモリ領域で表現する。
    /// </summary>
    public class ArraysBoolMap
    {


        public ArraysBoolMap(DataTypes.IDataType dataType, List<UnPackedArray>? unpackedArrays)
        {
            int bits = 1;
            if (dataType.PartSelectable && dataType.BitWidth != null) bits = (int)dataType.BitWidth;
            initialize(bits, dataType.PackedDimensions, unpackedArrays);
        }

        public ArraysBoolMap(List<PackedArray> packedArrays, List<UnPackedArray>? unpackedArrays)
        {
            initialize(1, packedArrays, unpackedArrays);
        }

        public ArraysBoolMap(int bits, List<PackedArray> packedArrays, List<UnPackedArray>? unpackedArrays)
        {
            initialize(bits, packedArrays, unpackedArrays);
        }
        protected ArraysBoolMap()
        {

        }

        private void initialize(int bits, List<PackedArray> packedArrays, List<UnPackedArray>? unpackedArrays)
        {
            mapRanges = null;
            size = bits;
            dimensions.Add(bits);

            foreach (PackedArray packedArray in packedArrays)
            {
                if (packedArray.Size == null) return;
                dimensions.Add((int)packedArray.Size);
                size = size * (int)packedArray.Size;
            }

            if (unpackedArrays != null)
            {
                foreach (UnPackedArray unpackedArray in unpackedArrays)
                {
                    if (unpackedArray.Size == null) return;
                    dimensions.Add((int)unpackedArray.Size);
                    size = size * (int)unpackedArray.Size;
                }
            }
            mapRanges = new List<mapRange>(size);
        }

        public ArraysBoolMap Clone()
        {
            ArraysBoolMap map = new ArraysBoolMap() { dimensions = dimensions, mapRanges = new List<mapRange>(size), size = size };
            return map; 
        }

        List<int> dimensions = new List<int>();
        private List<mapRange>? mapRanges = null;
        int size = 1;

        public void Assert(long startBit,long lastBit)
        {
            addRange(startBit, lastBit);
        }
        public void Assert(List<RangeExpression> rangeExpressions)
        {
            if (mapRanges == null) return;
            if (!TryResolveRange(rangeExpressions, out long start, out long last)) return;
            addRange(start, last);
        }

        /// <summary>
        /// range expression の並び (bit-select / part-select) をこのマップの
        /// フラットなビット範囲 [start, last] に変換する。選択が無ければ
        /// マップ全体。インデックスが定数に解決できない場合は false を返す
        /// (範囲不明を呼び出し元に伝えるため、Assert では「何もしない」と
        /// 同じ扱いになる)。
        /// </summary>
        public bool TryResolveRange(List<RangeExpression> rangeExpressions, out long start, out long last)
        {
            start = 0;
            last = size - 1;
            if (dimensions.Count == 0) return false;

            int dimension = dimensions.Count - 1;
            int dsize = size;

            foreach (RangeExpression rangeExpression in rangeExpressions)
            {
                if (dimension < 0) break;
                dsize = dsize / dimensions[dimension];

                if (rangeExpression is SingleBitRangeExpression)
                {
                    SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                    if (singleBitRangeExpression.BitIndex == null) return false;
                    start = start + dsize * (long)singleBitRangeExpression.BitIndex;
                    last = start + dsize - 1;
                    dimension--;
                }
                else if (rangeExpression is AbsoluteRangeExpression)
                {
                    AbsoluteRangeExpression absoluteRangeExpression = (AbsoluteRangeExpression)rangeExpression;
                    if (absoluteRangeExpression.MaxBitIndex == null) return false;
                    if (absoluteRangeExpression.MinBitIndex == null) return false;
                    start = start + dsize * (long)absoluteRangeExpression.MinBitIndex;
                    last = start + dsize * ((long)absoluteRangeExpression.MaxBitIndex - (long)absoluteRangeExpression.MinBitIndex);
                    dimension--;
                }
                else
                {
                    //throw new Exception();
                }
            }
            return true;
        }

        /// <summary>
        /// 指定ビット範囲をマップ済みにし、既にマップ済みだった (= 同じビットを
        /// 二重にドライブしている) かどうかを返す。連続代入の多重ドライブ判定
        /// (IEEE 1800-2017 6.5) で、bit-select / part-select が互いに素な
        /// 複数の連続代入を誤検出しないために使用する。
        /// </summary>
        public bool AssertWithOverlapCheck(long start, long last)
        {
            if (mapRanges == null) return false;

            bool overlapped = false;
            foreach (mapRange range in mapRanges)
            {
                if (range.Start <= last && start <= range.Last)
                {
                    overlapped = true;
                    break;
                }
            }
            addRange(start, last);
            return overlapped;
        }

        /// <summary>
        /// 全bitをtrueにする。
        /// </summary>
        public void AssertAll()
        {
            addRange(0, size - 1);
        }

        private void addRange(long start, long last)
        {
            if (mapRanges == null) return; //throw new Exception();
            // 1. 新しい範囲を追加
            mapRanges.Add(new mapRange(start, last));

            // 2. 開始位置でソート
            mapRanges.Sort();

            // 3. 重複・隣接する範囲をマージ
            var merged = new List<mapRange>();
            if (mapRanges.Count == 0) return;

            var current = mapRanges[0];
            for (int i = 1; i < mapRanges.Count; i++)
            {
                // 隣接(current.End + 1)または重複している場合
                if (mapRanges[i].Start <= current.Last + 1)
                {
                    current.Last = Math.Max(current.Last, mapRanges[i].Last);
                }
                else
                {
                    merged.Add(current);
                    current = mapRanges[i];
                }
            }
            merged.Add(current);
            mapRanges = merged;
        }

        /// <summary>
        /// 全bitがtrueかどうかを確認する。
        /// </summary>
        /// <returns></returns>
        public bool? IsFullMapped()
        {
            if (mapRanges == null) return null;
            return mapRanges.Count == 1 &&
                   mapRanges[0].Start == 0 &&
                   mapRanges[0].Last == size - 1;
        }
        public struct mapRange : IComparable<mapRange>
        {
            public long Start;
            public long Last;

            public mapRange(long start, long end) { Start = start; Last = end; }
            public int CompareTo(mapRange other) => Start.CompareTo(other.Start);
        }
    }
}
