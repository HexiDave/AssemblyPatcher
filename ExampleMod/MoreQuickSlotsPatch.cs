﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ExampleMod
{
    class MoreQuickSlotsPatch : BootstrapLib.IPatch
    {
        static int TopExistingSlot = 5;
        static int QuickSlotsToAdd = 5;
        static string[] DefaultKeysForSlots =
        {
            "6",
            "7",
            "8",
            "9",
            "0",
            "-",
            "="
        };

        public bool InitializePatch(ModuleDefMD module)
        {
            CreateButtonSlotEnums(module);
            return true;
        }

        public void CreateButtonSlotEnums(ModuleDef module)
        {
            // GameInput -> Button
            var gameInputType = module.Find("GameInput", false);
            var gameInputButtonType = module.Find("GameInput/Button", false);
            var maxButtonValue = gameInputButtonType.Fields.Where(f => f.Constant != null).Max(f => (int)f.Constant.Value);
            var buttonTypeSig = gameInputButtonType.GetEnumUnderlyingType();

            // QuickSlots
            var quickSlotStaticConstructor = module.Find("QuickSlots", false).FindStaticConstructor();

            var totalQuickSlots = TopExistingSlot + QuickSlotsToAdd;
            var quickSlotInstructions = quickSlotStaticConstructor.Body.Instructions;

            // Update the array size instruction
            quickSlotInstructions[0] = OpCodes.Ldc_I4.ToInstruction(totalQuickSlots + 1); // Add one due to a hitch in Subnautica's code

            // Finally, set up the default keyboard shortcuts
            var defaultKeyboardBindingsMethod = gameInputType.FindMethod("SetupDefaultKeyboardBindings");

            var bindingsInstructions = defaultKeyboardBindingsMethod.Body.Instructions;
            var callInstruction = bindingsInstructions[bindingsInstructions.Count - 2]; // Second to last instruction

            for (var i = 0; i < QuickSlotsToAdd; i++)
            {
                int nextSlotIndex = TopExistingSlot + i + 1;
                string slotName = string.Format("Slot{0}", nextSlotIndex);
                string quickSlotName = string.Format("QuickSlot{0}", nextSlotIndex);
                int nextSlotValue = maxButtonValue + i + 1;

                // Add the Button enum value
                FieldDef fieldToAdd = new FieldDefUser(slotName, new FieldSig(new ValueTypeSig(gameInputButtonType)))
                {
                    Constant = module.UpdateRowId(new ConstantUser(nextSlotValue, buttonTypeSig.ElementType)),
                    Attributes = FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.HasDefault | FieldAttributes.Public
                };

                gameInputButtonType.Fields.Add(fieldToAdd);

                // Add the QuickSlot name
                int insertPoint = quickSlotInstructions.Count - 2; // Ignore Stsfld and ret

                // dup
                // ldc.i4.s <nextSlotIndex>
                // ldstr <quickSlotName>
                // stelem.ref

                quickSlotInstructions.Insert(insertPoint++, OpCodes.Dup.ToInstruction());
                quickSlotInstructions.Insert(insertPoint++, OpCodes.Ldc_I4.ToInstruction(nextSlotIndex));
                quickSlotInstructions.Insert(insertPoint++, OpCodes.Ldstr.ToInstruction(quickSlotName));
                quickSlotInstructions.Insert(insertPoint++, OpCodes.Stelem_Ref.ToInstruction());

                // Insert the keyboard default
                if (i < DefaultKeysForSlots.Length)
                {
                    insertPoint = bindingsInstructions.Count - 1;

                    bindingsInstructions.Insert(insertPoint++, OpCodes.Ldc_I4_0.ToInstruction());
                    bindingsInstructions.Insert(insertPoint++, OpCodes.Ldc_I4.ToInstruction(nextSlotValue));
                    bindingsInstructions.Insert(insertPoint++, OpCodes.Ldc_I4_0.ToInstruction());
                    bindingsInstructions.Insert(insertPoint++, OpCodes.Ldstr.ToInstruction(DefaultKeysForSlots[i]));
                    bindingsInstructions.Insert(insertPoint++, callInstruction);
                }
            }

            // Rebuild the list of quick slot buttons in the Player
            RebuildPlayerQuickSlotList(module, totalQuickSlots, gameInputButtonType.Fields);

            // Update the quickslot count during inventory creation
            AdjustInventorySlotsCount(module, totalQuickSlots);
        }

        public void RebuildPlayerQuickSlotList(ModuleDef module, int totalQuickSlots, IList<FieldDef> buttonTypeFields)
        {
            // FIXME: There's a serious problem when decompiling the Player static constructor - it just explode.
            // For now, I'm just building a new one from scratch - a new game in Creative Mode will not have the starting gear.

            // Player
            var playerType = module.Find("Player", false);
            playerType.Attributes &= ~TypeAttributes.BeforeFieldInit;
            var playerStaticConstructor = playerType.FindStaticConstructor();

            var creativeEquipmentMember = playerType.FindField("creativeEquipment");
            var wantInterpolateMember = playerType.FindField("wantInterpolate");
            var quickSlotButtonsMember = playerType.FindField("quickSlotButtons");
            var quickSlotButtonsCountMember = playerType.FindField("quickSlotButtonsCount");

            var body = playerStaticConstructor.Body = new CilBody();
            
            var instructions = playerStaticConstructor.Body.Instructions;

            // Null out the Creative starter equipment array for now
            instructions.Add(OpCodes.Ldnull.ToInstruction());
            instructions.Add(OpCodes.Stsfld.ToInstruction(creativeEquipmentMember));

            // Set this to true
            instructions.Add(OpCodes.Ldc_I4_1.ToInstruction());
            instructions.Add(OpCodes.Stsfld.ToInstruction(wantInterpolateMember));
            
            // Create the Quick Slot array
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(totalQuickSlots));
            instructions.Add(OpCodes.Newarr.ToInstruction(buttonTypeFields[1].FieldType.ToTypeDefOrRef()));
            instructions.Add(OpCodes.Dup.ToInstruction());

            // Insert new instructions
            for (var i = 0; i < totalQuickSlots; i++)
            {
                string slotName = string.Format("Slot{0}", i + 1);
                var slotField = buttonTypeFields
                    .Where(x => x.Name == slotName)
                    .First();
                var slotValue = (int)slotField.Constant.Value;

                //                ldc.i4.0
                //117 015E    ldc.i4.8
                //118 015F    stelem.i4
                //119 0160    dup

                instructions.Add(OpCodes.Ldc_I4.ToInstruction(i));
                instructions.Add(OpCodes.Ldc_I4.ToInstruction(slotValue));
                instructions.Add(OpCodes.Stelem_I4.ToInstruction());
                // Skip on the last round
                if (i + 1 < totalQuickSlots)
                {
                    instructions.Add(OpCodes.Dup.ToInstruction());
                }
            }

            instructions.Add(OpCodes.Stsfld.ToInstruction(quickSlotButtonsMember));

            // Set the count
            instructions.Add(OpCodes.Ldc_I4.ToInstruction(totalQuickSlots));
            instructions.Add(OpCodes.Stsfld.ToInstruction(quickSlotButtonsCountMember));

            // return
            instructions.Add(OpCodes.Ret.ToInstruction());
        }

        public void AdjustInventorySlotsCount(ModuleDef module, int totalQuickSlots)
        {
            var inventoryAwake = module.Find("Inventory", false).FindMethod("Awake");
            var inventoryInstructions = inventoryAwake.Body.Instructions;

            // TODO: Refactor this with something in PatchHelper
            var instructionIndex = inventoryInstructions
                .Select((instruction, index) => new { instruction, index })
                .Where(x => x.instruction.OpCode == OpCodes.Ldfld && (x.instruction.Operand as IMemberRef).Name == "rightHandSlot")
                .First().index + 1; // We want the value after this one

            inventoryInstructions[instructionIndex] = OpCodes.Ldc_I4.ToInstruction(totalQuickSlots);
        }
    }
}
