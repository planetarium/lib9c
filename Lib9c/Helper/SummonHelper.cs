using System.Linq;
using Libplanet.Action;
using Nekoyume.TableData.Summon;

namespace Nekoyume.Helper
{
    public static class SummonHelper
    {
        public static readonly int[] AllowedSummonCount = {1, 10, 100};

        /// <summary>
        /// summon에서 쓸 count를 넣으면 허용된 count인지 체크한 뒤 허용 여부를 반환합니다.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static bool CheckSummonCountIsValid(int count)
        {
            return AllowedSummonCount.Contains(count);
        }

        /// <summary>
        /// count를 넣으면 10+1 이라는 규칙에 의해 증가된 값을 반환합니다.
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public static int CalculateSummonCount(int count)
        {
            return count + count / 10;
        }

        public static int GetSummonRecipeIdByRandom(SummonSheet.Row summonRow,
            IRandom random)
        {
            var targetRatio = random.Next(1, summonRow.TotalRatio() + 1);
            for (var j = 1; j <= SummonSheet.Row.MaxRecipeCount; j++)
            {
                if (targetRatio <= summonRow.CumulativeRatio(j))
                {
                    return summonRow.Recipes[j - 1].Item1;
                }
            }

            return summonRow.Recipes.First().Item1;
        }
    }
}
