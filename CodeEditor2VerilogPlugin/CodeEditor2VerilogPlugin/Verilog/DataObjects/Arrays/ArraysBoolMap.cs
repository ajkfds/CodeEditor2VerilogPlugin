using pluginVerilog.Verilog.DataObjects.Variables;
using pluginVerilog.Verilog.Expressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace pluginVerilog.Verilog.DataObjects.Arrays
{
    public class ArraysBoolMap
    {
        public ArraysBoolMap(List<PackedArray> packedArrays,List<UnPackedArray> unpackedArrays):this(1,packedArrays,unpackedArrays)
        {
        }
        public ArraysBoolMap(int bits, List<PackedArray> packedArrays, List<UnPackedArray> unpackedArrays)
        {
            mapRanges = null;
            int size = bits;
            dimensions.Add(bits);

            foreach (PackedArray packedArray in packedArrays)
            {
                if (packedArray.Size == null) return;
                dimensions.Add((int)packedArray.Size);
                size = size * (int)packedArray.Size;
            }
            foreach (UnPackedArray unpackedArray in unpackedArrays)
            {
                if (unpackedArray.Size == null) return;
                dimensions.Add((int)unpackedArray.Size);
                size = size + (int)unpackedArray.Size;
            }
            mapRanges = new List<mapRange>(size);
        }
        List<int> dimensions = new List<int>();
        private List<mapRange>? mapRanges = null;

        public void Assert(List<RangeExpression> rangeExpressions)
        {
            if (mapRanges == null) return;
            long start = 0;
            long last = mapRanges.Count-1;
            int dimension = 0;

            foreach(RangeExpression rangeExpression in rangeExpressions)
            {
                if(rangeExpression is SingleBitRangeExpression)
                {
                    SingleBitRangeExpression singleBitRangeExpression = (SingleBitRangeExpression)rangeExpression;
                    if (singleBitRangeExpression.BitIndex == null) return;
                    start = start + (long)dimensions[dimension]*(long)singleBitRangeExpression.BitIndex;
                    last = start + (long)dimensions[dimension] - 1;
                    dimension++;
                }
                else if(rangeExpression is AbsoluteRangeExpression)
                {
                    AbsoluteRangeExpression absoluteRangeExpression = (AbsoluteRangeExpression)rangeExpression;
                    if (absoluteRangeExpression.MaxBitIndex == null) return;
                    if (absoluteRangeExpression.MinBitIndex == null) return;
                    start = start + (long)dimensions[dimension] * (long)absoluteRangeExpression.MinBitIndex;
                    last = start + (long)dimensions[dimension]*((long)absoluteRangeExpression.MaxBitIndex- (long)absoluteRangeExpression.MinBitIndex) - 1;
                    dimension++;
                }
                else
                {
                    throw new Exception();
                }
            }
            addRange(start, last);
        }


        private void addRange(long start, long last)
        {
            if (mapRanges == null) throw new Exception();
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

        public bool? IsFullMapped()
        {
            if (mapRanges == null) return null;
            return mapRanges.Count == 1 &&
                   mapRanges[0].Start == 0 &&
                   mapRanges[0].Last == mapRanges.Count - 1;
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
