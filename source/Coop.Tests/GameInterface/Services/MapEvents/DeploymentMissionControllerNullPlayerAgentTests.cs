using GameInterface.Services.MapEvents.Patches;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using TaleWorlds.MountAndBlade;
using Xunit;

namespace Coop.Tests.GameInterface.Services.MapEvents;

/// <summary>
/// #4153: a coop deployment whose local hero has no agent on the field must not dereference the null
/// InitialPlayerAgent anywhere in the native 1.4.8 deployment methods the patch rewrites.
/// </summary>
public class DeploymentMissionControllerNullPlayerAgentTests
{
    [Theory]
    [InlineData("SetupTeams", 3)]
    [InlineData(nameof(DeploymentMissionController.FinishDeployment), 2)]
    public void NativeDeploymentMethod_LeavesNoAgentCallOnTheInitialPlayerAgent(string methodName, int guardedCalls)
    {
        var original = AccessTools.Method(typeof(DeploymentMissionController), methodName);
        var nativeCalls = AgentInstanceCalls(PatchProcessor.GetOriginalInstructions(original));
        // The InitialPlayerAgent read is the only Agent these methods call into, so every Agent call is on it
        Assert.Equal(guardedCalls, nativeCalls.Length);

        var patched = DeploymentSetupTeamsNullAgentPatch
            .Transpiler(PatchProcessor.GetOriginalInstructions(original), original)
            .ToList();

        Assert.Empty(AgentInstanceCalls(patched));
        Assert.Equal(guardedCalls, patched.Count(code =>
            code.operand is MethodInfo method && method.DeclaringType == typeof(DeploymentSetupTeamsNullAgentPatch)));
    }

    [Fact]
    public void NativeDeploymentMethods_ReadTheInitialPlayerAgentOnce()
    {
        var getter = AccessTools.PropertyGetter(typeof(Mission), nameof(Mission.InitialPlayerAgent));
        foreach (var name in new[] { "SetupTeams", nameof(DeploymentMissionController.FinishDeployment) })
        {
            var instructions = PatchProcessor.GetOriginalInstructions(
                AccessTools.Method(typeof(DeploymentMissionController), name));
            Assert.Single(instructions, code => code.operand is MethodInfo method && method == getter);
        }
    }

    private static MethodInfo[] AgentInstanceCalls(IEnumerable<CodeInstruction> instructions) =>
        instructions
            .Where(code => code.opcode == OpCodes.Call || code.opcode == OpCodes.Callvirt)
            .Select(code => code.operand as MethodInfo)
            .Where(method => method != null && !method.IsStatic && method.DeclaringType == typeof(Agent))
            .ToArray();
}
