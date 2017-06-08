using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Obfuscator.Structure
{
    public class StringHider
    {
        private Project project;
        private Assembly assembly;
        private IEnumerable<StringInstruction> stringInstructions;
        private MethodDefinition baseStringMethod;
        private TypeDefinition stringHiderType;
        private readonly Dictionary<string, MethodDefinition> _methodByString = new Dictionary<string, MethodDefinition>();
        private readonly List<byte> _dataBytes = new List<byte>();

        public StringHider(Project project, Assembly assembly, IEnumerable<StringInstruction> instructions)
        {
            this.project = project;
            this.assembly = assembly;
            stringInstructions = instructions;
        }

        public void HideStrings()
        {
            stringHiderType = new TypeDefinition(
                "<PPP>{" + Guid.NewGuid().ToString().ToUpper() + "}",
                Guid.NewGuid().ToString().ToUpper(),
                TypeAttributes.BeforeFieldInit | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
                assembly.TypeSystem.Object
            );
            var dataField = new FieldDefinition(
                "\0\0",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
                new ArrayType(assembly.TypeSystem.Byte)
            );
            var stringArrayField = new FieldDefinition(
                "\0\0\0",
                FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
                new ArrayType(assembly.TypeSystem.String)
            );
            stringHiderType.Fields.Add(dataField);
            stringHiderType.Fields.Add(stringArrayField);

            baseStringMethod = CreateBaseGetStringMethod(dataField, stringArrayField);
            FinalizeStringHiderClass(HideStrings(stringArrayField), stringArrayField, dataField);
        }

        private MethodDefinition CreateIndividualStringMethod(string methodName, FieldDefinition stringArrayField, int stringIndex, int start, int count)
        {
            var result = new MethodDefinition(
                methodName,
                MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
                assembly.TypeSystem.String);
            result.Body = new MethodBody(result);
            var processor = result.Body.GetILProcessor();
            var end = processor.Create(OpCodes.Ret);

            processor.Emit(OpCodes.Ldsfld, stringArrayField);
            processor.Emit(OpCodes.Ldc_I4, stringIndex);
            processor.Emit(OpCodes.Ldelem_Ref);
            processor.Emit(OpCodes.Dup);
            processor.Emit(OpCodes.Brtrue, end);
            processor.Emit(OpCodes.Pop);
            processor.Emit(OpCodes.Ldc_I4, stringIndex);
            processor.Emit(OpCodes.Ldc_I4, start);
            processor.Emit(OpCodes.Ldc_I4, count);
            processor.Emit(OpCodes.Call, baseStringMethod);
            processor.Append(end);
            return result;
        }

        private MethodDefinition CreateBaseGetStringMethod(FieldDefinition dataField, FieldDefinition stringArrayField)
        {
            var systemIntTypeReference = assembly.TypeSystem.Int32;

            var encodingType = typeof(Encoding);
            var method1 = assembly.Import(encodingType.GetMethod("get_UTF8"));
            var method2 = assembly.Import(encodingType.GetMethod("GetString", new[] { typeof(byte[]), typeof(int), typeof(int) }));
            // Add method to extract a string from the byte array. It is called by the indiviual string getter methods we add later to the class.
            var result = new MethodDefinition(
                "\0",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
                assembly.TypeSystem.String
            );
            result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
            result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
            result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
            result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.String));
            var processor = result.Body.GetILProcessor();

            processor.Emit(OpCodes.Call, method1);
            processor.Emit(OpCodes.Ldsfld, dataField);
            processor.Emit(OpCodes.Ldarg_1);
            processor.Emit(OpCodes.Ldarg_2);
            processor.Emit(OpCodes.Callvirt, method2);
            processor.Emit(OpCodes.Stloc_0);

            processor.Emit(OpCodes.Ldsfld, stringArrayField);
            processor.Emit(OpCodes.Ldarg_0);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Stelem_Ref);

            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Ret);
            stringHiderType.Methods.Add(result);
            return result;
        }

        private void FinalizeStringHiderClass(int stringIndex, FieldDefinition stringArrayField, FieldDefinition dataField)
        {
            var structType = new TypeDefinition(
                "\0",
                "",
                TypeAttributes.ExplicitLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate,
                assembly.Import(typeof (ValueType)))
            {
                PackingSize = 1
            };

            var dataConstantField = new FieldDefinition(
                "\0", 
                FieldAttributes.HasFieldRVA | FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly, 
                structType
            );
            stringHiderType.NestedTypes.Add(structType);
            stringHiderType.Fields.Add(dataConstantField);
            structType.ClassSize = _dataBytes.Count;

            for (var i = 0; i < _dataBytes.Count; i++)
            {
                _dataBytes[i] = (byte)(_dataBytes[i] ^ (byte)i ^ 0xAA);
            }
            dataConstantField.InitialValue = _dataBytes.ToArray();

            var initializeArrayRef = assembly.Import(typeof(RuntimeHelpers).GetMethod("InitializeArray"));
            var staticConstructor = new MethodDefinition(
                ".cctor",
                MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                assembly.TypeSystem.Void
            );
            stringHiderType.Methods.Add(staticConstructor);
            staticConstructor.Body = new MethodBody(staticConstructor);
            staticConstructor.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));

            var processor = staticConstructor.Body.GetILProcessor();
            processor.Emit(OpCodes.Ldc_I4, stringIndex);
            processor.Emit(OpCodes.Newarr, assembly.TypeSystem.String);
            processor.Emit(OpCodes.Stsfld, stringArrayField);
            processor.Emit(OpCodes.Ldc_I4, _dataBytes.Count);
            processor.Emit(OpCodes.Newarr, assembly.TypeSystem.Byte);
            processor.Emit(OpCodes.Dup);
            processor.Emit(OpCodes.Ldtoken, dataConstantField);
            processor.Emit(OpCodes.Call, initializeArrayRef);
            processor.Emit(OpCodes.Stsfld, dataField);
            processor.Emit(OpCodes.Ldc_I4_0);
            processor.Emit(OpCodes.Stloc_0);
            var backlabel1 = processor.Create(OpCodes.Br_S, staticConstructor.Body.Instructions[0]);
            processor.Append(backlabel1);
            var label2 = processor.Create(OpCodes.Ldsfld, dataField);
            processor.Append(label2);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Ldsfld, dataField);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Ldelem_U1);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Xor);
            processor.Emit(OpCodes.Ldc_I4, 0xAA);
            processor.Emit(OpCodes.Xor);
            processor.Emit(OpCodes.Conv_U1);
            processor.Emit(OpCodes.Stelem_I1);
            processor.Emit(OpCodes.Ldloc_0);
            processor.Emit(OpCodes.Ldc_I4_1);
            processor.Emit(OpCodes.Add);
            processor.Emit(OpCodes.Stloc_0);
            backlabel1.Operand = processor.Create(OpCodes.Ldloc_0);
            processor.Append((Instruction)backlabel1.Operand);
            processor.Emit(OpCodes.Ldsfld, dataField);
            processor.Emit(OpCodes.Ldlen);
            processor.Emit(OpCodes.Conv_I4);
            processor.Emit(OpCodes.Clt);
            processor.Emit(OpCodes.Brtrue, label2);
            processor.Emit(OpCodes.Ret);

            assembly.AddType(stringHiderType);
        }

        private int HideStrings(FieldDefinition stringArrayField)
        {
            var nameInterator = project.NameIteratorFabric.GetIterator();
            var stringIndex = 0;
            foreach (var instruction in stringInstructions)
            {
                MethodDefinition individualStringMethodDefinition;
                if (!_methodByString.TryGetValue(instruction.String, out individualStringMethodDefinition))
                {
                    var methodName = nameInterator.Next();

                    var start = _dataBytes.Count;
                    _dataBytes.AddRange(Encoding.UTF8.GetBytes(instruction.String));
                    var count = _dataBytes.Count - start;

                    individualStringMethodDefinition = CreateIndividualStringMethod(methodName, stringArrayField, stringIndex, start, count);
                    stringHiderType.Methods.Add(individualStringMethodDefinition);
                    _methodByString.Add(instruction.String, individualStringMethodDefinition);

                    stringIndex++;
                }

                var newinstruction = Instruction.Create(OpCodes.Call, individualStringMethodDefinition);
                instruction.ReplaceStringWithInstruction(newinstruction);
            }
            return stringIndex;
        }
    }
}
