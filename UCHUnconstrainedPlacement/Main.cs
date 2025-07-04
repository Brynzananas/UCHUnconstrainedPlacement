using BepInEx;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
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
        public const string ModVer = "1.2.0";
        private Harmony harmonyPatcher;
        public void Awake()
        {
            harmonyPatcher = new Harmony(ModGuid);
            harmonyPatcher.CreateClassProcessor(typeof(Patches)).Patch();
        }
        public static void RemovePlacementRounding(ILCursor iLCursor)
        {
            while (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchCall<Mathf>(nameof(Mathf.Round))
                ))
            {
                iLCursor.Remove();
            }
        }
        public static void RemoveRotationRounding(ILCursor iLCursor)
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
                Debug.LogError(iLCursor.Context.Method.Name + " IL hook failed");
            }
        }
        public static void SetRotationOverride(ILCursor iLCursor)
        {
            ILLabel iLLabel = null;
            if (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchBrfalse(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall<PiecePlacementCursor>("get_Piece"),
                x => x.MatchLdfld<Placeable>(nameof(Placeable.OrientationAlt)),
                x => x.MatchLdcI4(out _),
                x => x.MatchBneUn(out iLLabel),
                x => x.MatchLdarg(0),
                x => x.MatchCall<PiecePlacementCursor>("get_Piece"),
                x => x.MatchLdfld<Placeable>(nameof(Placeable.Orientation)),
                x => x.MatchBr(out _),
                x => x.MatchLdarg(0),
                x => x.MatchCall<PiecePlacementCursor>("get_Piece"),
                x => x.MatchLdfld<Placeable>(nameof(Placeable.OrientationAlt)),
                x => x.MatchStloc(1),
                x => x.MatchLdloc(1),
                x => x.MatchSwitch(out _)
                ))
            {
                iLCursor.Index += 1;
                iLCursor.RemoveRange(6);
                iLCursor.Emit(OpCodes.Brtrue_S, iLLabel);
                iLCursor.Index += 3;
                iLCursor.Emit(OpCodes.Pop);
                iLCursor.Emit(OpCodes.Ldc_I4, (int)Placeable.OrientMode.ROTATE);
                iLCursor.Index += 4;
                iLCursor.Emit(OpCodes.Pop);
                iLCursor.Emit(OpCodes.Ldc_I4, (int)Placeable.OrientMode.FLIPX);
            }
            else
            {
                Debug.LogError(iLCursor.Context.Method.Name + " IL hook failed");
            }
        }
    }
    [HarmonyPatch]
    class Patches
    {
        [HarmonyPatch(typeof(QuickSaver), nameof(QuickSaver.RestoreSaveables))]
        [HarmonyILManipulator]
        private static void QuickSaver_RestoreSaveables(ILContext il)
        {
            ILCursor iLCursor = new ILCursor(il);
            while (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloca(out _),
                x => x.MatchLdloc(out _),
                x => x.MatchLdfld<Vector3>(nameof(Vector3.z)),
                x => x.MatchLdcR4(90f),
                x => x.MatchDiv(),
                x => x.MatchCall<Mathf>(nameof(Mathf.Round)),
                x => x.MatchLdcR4(90f),
                x => x.MatchMul(),
                x => x.MatchStfld<Vector3>(nameof(Vector3.z))
                ))
            {
                iLCursor.RemoveRange(9);
            }
        }
        [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.FixedUpdate))]
        [HarmonyILManipulator]
        private static void PiecePlacementCursor_FixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Main.RemovePlacementRounding(c);
        }
        [HarmonyPatch(typeof(Placeable), nameof(Placeable.Place), new Type[] { typeof(int), typeof(bool), typeof(bool) })]
        [HarmonyILManipulator]
        private static void Placeable_Place_int_bool_bool(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Main.RemoveRotationRounding(c);
        }
        [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.SetPiece))]
        [HarmonyILManipulator]
        private static void PiecePlacementCursor_SetPiece(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Main.RemoveRotationRounding(c);
            c = new ILCursor(il);
            Main.RemovePlacementRounding(c);
        }
        [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.PerformRightRotation))]
        [HarmonyILManipulator]
        private static void PiecePlacementCursor_PerformRightRotation(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Main.SetRotationOverride(c);
            c = new ILCursor(il);
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
        [HarmonyPatch(typeof(PiecePlacementCursor), nameof(PiecePlacementCursor.PerformLeftRotation))]
        [HarmonyILManipulator]
        private static void PiecePlacementCursor_PerformLeftRotation(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            Main.SetRotationOverride(c);
            c = new ILCursor(il);
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
        [HarmonyPatch(typeof(SwingingAxe), nameof(SwingingAxe.Update))]
        [HarmonyILManipulator]
        private static void SwingingAxe_Update(ILContext il)
        {
            ILCursor iLCursor = new ILCursor(il);
            if (iLCursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<SwingingAxe>(nameof(SwingingAxe.RotateLocked)),
                x => x.MatchCallvirt<Component>("get_transform"),
                x => x.MatchCall<Quaternion>("get_identity"),
                x => x.MatchCallvirt<Transform>("set_rotation")
                ))
            {
                iLCursor.RemoveRange(5);
            }
            else
            {
                Debug.LogError(iLCursor.Context.Method.Name + " IL hook failed");
            }
        }
    }
}