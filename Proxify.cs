using HarmonyLib;
using NeosModLoader;
using FrooxEngine;
using FrooxEngine.LogiX;
using FrooxEngine.LogiX.Data;
using System.Reflection;
using BaseX;
using CodeX;
using System;

namespace Proxify;
public class Proxify : NeosMod
{
    public override string Author => "Cyro";
    public override string Name => "Proxify";
    public override string Version => "1.0.0";
    public override void OnEngineInit()
    {
        Harmony harmony = new Harmony("net.Cyro.Proxify");
        harmony.PatchAll();
    }

    public static bool IsRegister(ComponentBase<Component> instance)
    {
        bool isGeneric = instance.GetType().IsGenericType;
        bool isTarget =  instance is SlotRegister || instance is UserRegister;
        if (isGeneric)
        {
            isTarget = instance.GetType().GetGenericTypeDefinition() == typeof(ReferenceRegister<>) || instance.GetType().GetGenericTypeDefinition() == typeof(ValueRegister<>);
        }
        return isTarget;
    }
    
    // I hate doing this. Avoid these patches wherever possible as they can have unexpected side-effects. I'm only doing this because harmony sucks at patching generic objects and methods.
    [HarmonyPatch(typeof(ComponentBase<Component>))]
    public static class ComponentBase_OnAttach_Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("OnAttach")]
        static void Postfix1(ComponentBase<Component> __instance)
        {
            if (IsRegister(__instance))
            {
                Type type = __instance.GetType();
                FieldInfo? ValueField = type.GetField("Value", BindingFlags.Public | BindingFlags.Instance);
                IWorldElement? Value = null;
                if (ValueField != null)
                {
                    Value = (IWorldElement)ValueField.GetValue(__instance);
                }
                FieldInfo? RefField = type.GetField("Target", BindingFlags.Public | BindingFlags.Instance);
                IWorldElement? Ref = null;
                if (RefField != null)
                {
                    Ref = (IWorldElement)RefField.GetValue(__instance);
                }
                FieldInfo? UserField = type.GetField("User", BindingFlags.Public | BindingFlags.Instance);
                IWorldElement? User = null;
                if (UserField != null)
                {
                    Type UserSyncType = UserField.GetValue(__instance).GetType();
                    FieldInfo UserSyncField = UserSyncType.GetField("User", BindingFlags.Public | BindingFlags.Instance);
                    User = (IWorldElement)UserSyncField.GetValue(UserField.GetValue(__instance));
                }
                ReferenceProxy proxy = ((Component)__instance).Slot.GetComponentOrAttach<ReferenceProxy>((r) => r.Reference.Target == null);
                proxy.Reference.Target = Value ?? Ref ?? User;
            }
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch("OnDestroy")]
    static void Postfix2(ComponentBase<Component> __instance)
    {
        if (IsRegister(__instance))
        {
            ReferenceProxy proxy = ((Component)__instance).Slot.GetComponent<ReferenceProxy>();
            if (proxy != null)
            {
                if (((SyncElement)proxy.Reference.Target).Component == __instance)
                {
                    proxy.Destroy();
                }
            }
        }
    }
}
