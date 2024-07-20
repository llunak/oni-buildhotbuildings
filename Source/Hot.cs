using HarmonyLib;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace BuildHotBuildings
{
    [HarmonyPatch(typeof(Constructable))]
    public class Constructable_Patch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(nameof(OnCompleteWork))]
        public static IEnumerable<CodeInstruction> OnCompleteWork(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            bool found1 = false;
            bool found2 = false;
            for( int i = 0; i < codes.Count; ++i )
            {
//                Debug.Log("T:" + i + ":" + codes[i].opcode + "::" + (codes[i].operand != null ? codes[i].operand.ToString() : codes[i].operand));
                // The function has code:
                // initialTemperature = Mathf.Min(num2 / num, 318.15f);
                // Change to:
                // initialTemperature = num2 / num;
                if( codes[ i ].opcode == OpCodes.Ldc_R4 && codes[ i ].operand.ToString() == "318.15"
                    && i + 1 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Call
                    && codes[ i + 1 ].operand.ToString() == "Single Min(Single, Single)" )
                {
                    codes.RemoveAt( i ); // remove load of 318.15
                    codes.RemoveAt( i ); // remove call to Min()
                    found1 = true;
                }
                // The function has code:
                // initialTemperature = Mathf.Clamp(num2 / num, 288.15f, 318.15f);
                // Change to:
                // initialTemperature = num2 / num;
                if( codes[ i ].opcode == OpCodes.Ldc_R4
                    && i + 2 < codes.Count
                    && codes[ i + 1 ].opcode == OpCodes.Ldc_R4 && codes[ i + 1 ].operand.ToString() == "318.15"
                    && codes[ i + 2 ].opcode == OpCodes.Call
                    && codes[ i + 2 ].operand.ToString() == "Single Clamp(Single, Single, Single)" )
                {
                    codes.RemoveAt( i ); // remove load of 288.15 (or 0 since u52-621068)
                    codes.RemoveAt( i ); // remove load of 318.15
                    codes.RemoveAt( i ); // remove call to Clamp()
                    found2 = true;
                }
            }
            if(!found1 || !found2)
                Debug.LogWarning("BuildHotBuildings: Failed to patch Constructable.OnCompleteWork()");
            return codes;
        }
    }
}
