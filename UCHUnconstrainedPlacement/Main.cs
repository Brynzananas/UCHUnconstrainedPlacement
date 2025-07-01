using BepInEx;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System.Net;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[module: UnverifiableCode]
#pragma warning disable CS0618
#pragma warning restore CS0618
namespace UncontrainedPlacement
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [System.Serializable]
    public class Main : BaseUnityPlugin
    {
        public const string ModGuid = "com.brynzananas.uncontrainedplacement";
        public const string ModName = "Uncontrained Placement";
        public const string ModVer = "1.0.0";
        public void Awake()
        {
            IL.PiecePlacementCursor.PerformLeftRotation += PiecePlacementCursor_PerformLeftRotation;
            IL.PiecePlacementCursor.PerformRightRotation += PiecePlacementCursor_PerformRightRotation;
            IL.PiecePlacementCursor.SetPiece += PiecePlacementCursor_SetPiece;
            IL.PiecePlacementCursor.FixedUpdate += PiecePlacementCursor_FixedUpdate;
            IL.Placeable.Place_int_bool_bool += Placeable_Place_int_bool_bool;
        }
        private static void RemovePlacementRounding(ILCursor iLCursor)
        {
            while (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchCall<Mathf>(nameof(Mathf.Round))
                ))
            {
                iLCursor.Remove();
            }
        }
        private static void RemoveRotationRounding(ILCursor iLCursor, ILContext iLContext)
        {
            if (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchLdcR4(90f),
                x => x.MatchDiv(),
                x => x.MatchCall<Mathf>(nameof(Mathf.Round)),
                x => x.MatchLdcR4(90f),
                x => x.MatchMul(),
                x => x.MatchStloc(0)
                ))
            {
                iLCursor.RemoveRange(7);
            }
            else
            {
                Debug.LogError(iLContext.Method.Name + " IL hook failed");
            }
        }
        private void PiecePlacementCursor_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            RemovePlacementRounding(c);
        }

        private void Placeable_Place_int_bool_bool(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            RemoveRotationRounding(c, il);
        }
        private void PiecePlacementCursor_SetPiece(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            RemoveRotationRounding(c, il);
            c = new ILCursor(il);
            RemovePlacementRounding(c);
        }
        private void PiecePlacementCursor_PerformRightRotation(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(-90f)
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_R4, -15f);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL hook failed");
            }
        }
        private void PiecePlacementCursor_PerformLeftRotation(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            if (c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(90f)
                ))
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_R4, 15f);
            }
            else
            {
                Debug.LogError(il.Method.Name + " IL hook failed");
            }
        }
    }
}