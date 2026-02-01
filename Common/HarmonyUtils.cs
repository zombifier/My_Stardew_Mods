using HarmonyLib;
using System.Reflection.Emit;

using SObject = StardewValley.Object;

namespace Selph.StardewMods.Common;

public static class CodeInstructionExtensions {
  public static CodeInstruction StToLd(this CodeInstruction codeInstruction) {
    OpCode opcode = codeInstruction.opcode;
    if (codeInstruction.opcode == OpCodes.Stloc) {
      opcode = OpCodes.Ldloc;
    }
    if (codeInstruction.opcode == OpCodes.Stloc_S) {
      opcode = OpCodes.Ldloc_S;
    }
    if (codeInstruction.opcode == OpCodes.Stloc_0) {
      opcode = OpCodes.Ldloc_0;
    }
    if (codeInstruction.opcode == OpCodes.Stloc_1) {
      opcode = OpCodes.Ldloc_1;
    }
    if (codeInstruction.opcode == OpCodes.Stloc_2) {
      opcode = OpCodes.Ldloc_2;
    }
    if (codeInstruction.opcode == OpCodes.Stloc_3) {
      opcode = OpCodes.Ldloc_3;
    }
    return new(opcode, codeInstruction.operand);
  }

  public static CodeInstruction LdToSt(this CodeInstruction codeInstruction) {
    OpCode opcode = codeInstruction.opcode;
    if (codeInstruction.opcode == OpCodes.Ldloc) {
      opcode = OpCodes.Stloc;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_S) {
      opcode = OpCodes.Stloc_S;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_0) {
      opcode = OpCodes.Stloc_0;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_1) {
      opcode = OpCodes.Stloc_1;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_2) {
      opcode = OpCodes.Stloc_2;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_3) {
      opcode = OpCodes.Stloc_3;
    }
    return new(opcode, codeInstruction.operand);
  }

  public static CodeInstruction LdToLda(this CodeInstruction codeInstruction) {
    OpCode opcode = codeInstruction.opcode;
    object? operand = codeInstruction.operand;
    if (codeInstruction.IsLdloc()) {
      opcode = OpCodes.Ldloca_S;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc) {
      opcode = OpCodes.Ldloca;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_0) {
      operand = (byte)0;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_1) {
      operand = (byte)1;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_2) {
      operand = (byte)2;
    }
    if (codeInstruction.opcode == OpCodes.Ldloc_3) {
      operand = (byte)3;
    }
    return new(opcode, codeInstruction.operand);
  }
}
