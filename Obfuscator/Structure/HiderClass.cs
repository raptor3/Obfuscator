using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Obfuscator.Structure.Instrucitons;
using Obfuscator.Iterator;

namespace Obfuscator.Structure
{
	public class HiderClass
	{
		private Project project;
		private Assembly assembly;
		private MethodDefinition baseStringMethod;
		private TypeDefinition hiderType;
		private readonly Dictionary<string, MethodDefinition> _methodByString = new Dictionary<string, MethodDefinition>();
		private readonly List<byte> _dataBytes = new List<byte>();
		private FieldDefinition dataField;
		private FieldDefinition stringArrayField;
		private FieldDefinition doubleDictField;
		private FieldDefinition longDictField;
		private FieldDefinition floatDictField;
		private FieldDefinition intDictField;
		private FieldDefinition byteDictField;
		private int stringIndex;
		private INameIterator iterator;
		private MethodDefinition baseGetDouble;
		private MethodDefinition baseGetLong;
		private MethodDefinition baseGetFloat;
		private MethodDefinition baseGetInt;
		private MethodDefinition baseGetByte;
		private MethodDefinition baseReverseLong;
		private MethodDefinition baseReverseInt;
		private MethodDefinition baseReverseByte;

		public HiderClass(Project project, Assembly assembly)
		{
			this.project = project;
			this.assembly = assembly;
			iterator = project.NameIteratorFabric.GetIterator();
		}

		public void CreateHiderClass()
		{
			stringIndex = 0;
			hiderType = new TypeDefinition(
				iterator.Next(),
				iterator.Next(),
				TypeAttributes.BeforeFieldInit | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.BeforeFieldInit,
				assembly.TypeSystem.Object
			);
			dataField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				new ArrayType(assembly.TypeSystem.Byte)
			);
			stringArrayField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				new ArrayType(assembly.TypeSystem.String)
			);
			doubleDictField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				assembly.Import(typeof(Dictionary<double, double>))
			);
			longDictField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				assembly.Import(typeof(Dictionary<long, long>))
			);
			floatDictField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				assembly.Import(typeof(Dictionary<float, float>))
			);
			intDictField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				assembly.Import(typeof(Dictionary<int, int>))
			);
			byteDictField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				assembly.Import(typeof(Dictionary<byte, byte>))
			);

			hiderType.Fields.Add(dataField);
			hiderType.Fields.Add(stringArrayField);
			hiderType.Fields.Add(doubleDictField);
			hiderType.Fields.Add(longDictField);
			hiderType.Fields.Add(floatDictField);
			hiderType.Fields.Add(intDictField);
			hiderType.Fields.Add(byteDictField);

			baseStringMethod = CreateBaseGetStringMethod();
			baseReverseLong = CreateReverseLongMethod();
			baseGetDouble = CreateGetDoubleMethod();
			baseGetLong = CreateGetLongMethod();
			baseReverseInt = CreateReverseIntMethod();
			baseGetFloat = CreateGetFloatMethod();
			baseGetInt = CreateGetIntMethod();
			baseReverseByte = CreateReverseByteMethod();
			baseGetByte = CreateGetByteMethod();

			hiderType.Methods.Add(baseStringMethod);
			hiderType.Methods.Add(baseReverseLong);
			hiderType.Methods.Add(baseGetDouble);
			hiderType.Methods.Add(baseGetLong);
			hiderType.Methods.Add(baseReverseInt);
			hiderType.Methods.Add(baseGetFloat);
			hiderType.Methods.Add(baseGetInt);
			hiderType.Methods.Add(baseReverseByte);
			hiderType.Methods.Add(baseGetByte);
		}

		private MethodDefinition CreateIndividualStringMethod(string methodName, int stringIndex, int start, int count)
		{
			var result = new MethodDefinition(
				methodName,
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.String);
			result.Body = new MethodBody(result);
			var processor = result.Body.GetILProcessor();
			var end = processor.Create(OpCodes.Ret);

			#region CIL of individual get string method

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

			#endregion

			return result;
		}

		private MethodDefinition CreateBaseGetStringMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var encodingType = typeof(Encoding);
			var getUtf8 = assembly.Import(encodingType.GetMethod("get_UTF8"));
			var getString = assembly.Import(encodingType.GetMethod("GetString", new[] { typeof(byte[]), typeof(int), typeof(int) }));
			// Add method to extract a string from the byte array. It is called by the indiviual string getter methods we add later to the class.
			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				assembly.TypeSystem.String
			);
			result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
			result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
			result.Parameters.Add(new ParameterDefinition(systemIntTypeReference));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.String));
			var processor = result.Body.GetILProcessor();

			#region CIL of base get string method
			processor.Emit(OpCodes.Call, getUtf8);
			processor.Emit(OpCodes.Ldsfld, dataField);
			processor.Emit(OpCodes.Ldarg_1);
			processor.Emit(OpCodes.Ldarg_2);
			processor.Emit(OpCodes.Callvirt, getString);
			processor.Emit(OpCodes.Stloc_0);

			processor.Emit(OpCodes.Ldsfld, stringArrayField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stelem_Ref);

			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ret);
			#endregion
			return result;
		}

		private MethodDefinition CreateReverseLongMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var encodingType = typeof(Encoding);
			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				assembly.TypeSystem.Int64
			);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Int64));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.UInt64));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.UInt64));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			var v4 = new VariableDefinition(assembly.TypeSystem.Int64);
			result.Body.Variables.Add(v4);
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Not);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)63);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			var brsTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Br_S, brsTo);
			var brTrueTo = processor.Create(OpCodes.Nop);
			processor.Append(brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Conv_I8);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Or);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Sub);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Conv_I8);
			processor.Emit(OpCodes.Cgt_Un);
			processor.Emit(OpCodes.Stloc_3);
			processor.Emit(OpCodes.Ldloc_3);
			processor.Emit(OpCodes.Brtrue_S, brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)63);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Stloc_S, v4);
			var brsTo2 = processor.Create(OpCodes.Ldloc_S, v4);
			processor.Emit(OpCodes.Br_S, brsTo2);
			processor.Append(brsTo2);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateReverseIntMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var encodingType = typeof(Encoding);
			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				assembly.TypeSystem.Int32
			);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Int32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.UInt32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.UInt32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			var v4 = new VariableDefinition(assembly.TypeSystem.Int32);
			result.Body.Variables.Add(v4);
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Not);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)31);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			var brsTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Br_S, brsTo);
			var brTrueTo = processor.Create(OpCodes.Nop);
			processor.Append(brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Or);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Sub);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Conv_I8);
			processor.Emit(OpCodes.Cgt_Un);
			processor.Emit(OpCodes.Stloc_3);
			processor.Emit(OpCodes.Ldloc_3);
			processor.Emit(OpCodes.Brtrue_S, brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)31);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Stloc_S, v4);
			var brsTo2 = processor.Create(OpCodes.Ldloc_S, v4);
			processor.Emit(OpCodes.Br_S, brsTo2);
			processor.Append(brsTo2);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateReverseByteMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var encodingType = typeof(Encoding);
			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig,
				assembly.TypeSystem.Byte
			);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Byte));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Byte));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Byte));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			var v4 = new VariableDefinition(assembly.TypeSystem.Byte);
			result.Body.Variables.Add(v4);
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Not);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)7);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			var brsTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Br_S, brsTo);
			var brTrueTo = processor.Create(OpCodes.Nop);
			processor.Append(brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Conv_U1);
			processor.Emit(OpCodes.Or);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Sub);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Ldc_I4_1);
			processor.Emit(OpCodes.Shr_Un);
			processor.Emit(OpCodes.Stloc_0);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Cgt_Un);
			processor.Emit(OpCodes.Stloc_3);
			processor.Emit(OpCodes.Ldloc_3);
			processor.Emit(OpCodes.Brtrue_S, brTrueTo);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Ldc_I4_S, (sbyte)7);
			processor.Emit(OpCodes.And);
			processor.Emit(OpCodes.Shl);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			processor.Emit(OpCodes.Stloc_S, v4);
			var brsTo2 = processor.Create(OpCodes.Ldloc_S, v4);
			processor.Emit(OpCodes.Br_S, brsTo2);
			processor.Append(brsTo2);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateGetDoubleMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.Double
			);
			var dictType = typeof(Dictionary<double, double>);
			var bitConverterType = typeof(BitConverter);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Double));
			var variableDef = new VariableDefinition(assembly.TypeSystem.Double);
			result.Body.Variables.Add(variableDef);
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Double));
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldsfld, doubleDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloca_S, variableDef);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("TryGetValue")));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Ceq);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			var brFalsesTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Brfalse_S, brFalsesTo);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("DoubleToInt64Bits")));
			processor.Emit(OpCodes.Call, baseReverseLong);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("Int64BitsToDouble")));
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldsfld, doubleDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("Add")));
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_2);
			var brsTo = processor.Create(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brFalsesTo);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateGetLongMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.Int64
			);
			var dictType = typeof(Dictionary<long, long>);
			var bitConverterType = typeof(BitConverter);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Int64));
			var variableDef = new VariableDefinition(assembly.TypeSystem.Int64);
			result.Body.Variables.Add(variableDef);
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int64));
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldsfld, longDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloca_S, variableDef);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("TryGetValue")));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Ceq);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			var brFalsesTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Brfalse_S, brFalsesTo);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseReverseLong);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldsfld, longDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("Add")));
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_2);
			var brsTo = processor.Create(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brFalsesTo);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateGetFloatMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.Single
			);
			var dictType = typeof(Dictionary<float, float>);
			var bitConverterType = typeof(BitConverter);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Single));
			var variableDef = new VariableDefinition(assembly.TypeSystem.Single);
			result.Body.Variables.Add(variableDef);
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Single));
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldsfld, floatDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloca_S, variableDef);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("TryGetValue")));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Ceq);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			var brFalsesTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Brfalse_S, brFalsesTo);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("GetBytes", new[] { typeof(float)})));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("ToInt32")));
			processor.Emit(OpCodes.Call, baseReverseInt);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("GetBytes", new[] { typeof(int) })));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Call, assembly.Import(bitConverterType.GetMethod("ToSingle")));
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldsfld, floatDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("Add")));
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_2);
			var brsTo = processor.Create(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brFalsesTo);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateGetIntMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.Int32
			);
			var dictType = typeof(Dictionary<int, int>);
			var bitConverterType = typeof(BitConverter);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Int32));
			var variableDef = new VariableDefinition(assembly.TypeSystem.Int32);
			result.Body.Variables.Add(variableDef);
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldsfld, intDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloca_S, variableDef);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("TryGetValue")));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Ceq);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			var brFalsesTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Brfalse_S, brFalsesTo);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseReverseInt);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldsfld, intDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("Add")));
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_2);
			var brsTo = processor.Create(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brFalsesTo);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		private MethodDefinition CreateGetByteMethod()
		{
			var systemIntTypeReference = assembly.TypeSystem.Int32;

			var result = new MethodDefinition(
				iterator.Next(),
				MethodAttributes.Static | MethodAttributes.Public | MethodAttributes.HideBySig,
				assembly.TypeSystem.Byte
			);
			var dictType = typeof(Dictionary<byte, byte>);
			var bitConverterType = typeof(BitConverter);
			result.Parameters.Add(new ParameterDefinition(assembly.TypeSystem.Byte));
			var variableDef = new VariableDefinition(assembly.TypeSystem.Byte);
			result.Body.Variables.Add(variableDef);
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Boolean));
			result.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Byte));
			result.Body.InitLocals = true;
			var processor = result.Body.GetILProcessor();

			#region CIL of reverse long method
			processor.Emit(OpCodes.Ldsfld, byteDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloca_S, variableDef);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("TryGetValue")));
			processor.Emit(OpCodes.Ldc_I4_0);
			processor.Emit(OpCodes.Ceq);
			processor.Emit(OpCodes.Stloc_1);
			processor.Emit(OpCodes.Ldloc_1);
			var brFalsesTo = processor.Create(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Brfalse_S, brFalsesTo);
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Call, baseReverseByte);
			processor.Emit(OpCodes.Stloc_0);
			processor.Emit(OpCodes.Ldsfld, byteDictField);
			processor.Emit(OpCodes.Ldarg_0);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Callvirt, assembly.Import(dictType.GetMethod("Add")));
			processor.Emit(OpCodes.Nop);
			processor.Emit(OpCodes.Ldloc_0);
			processor.Emit(OpCodes.Stloc_2);
			var brsTo = processor.Create(OpCodes.Ldloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brFalsesTo);
			processor.Emit(OpCodes.Stloc_2);
			processor.Emit(OpCodes.Br_S, brsTo);
			processor.Append(brsTo);
			processor.Emit(OpCodes.Ret);
			#endregion

			return result;
		}

		public void FinalizeHiderClass()
		{
			var structType = new TypeDefinition(
				iterator.Next(),
				"",
				TypeAttributes.ExplicitLayout | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.NestedPrivate,
				assembly.Import(typeof(ValueType)))
			{
				PackingSize = 1
			};

			var dataConstantField = new FieldDefinition(
				iterator.Next(),
				FieldAttributes.HasFieldRVA | FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.Assembly,
				structType
			);
			hiderType.NestedTypes.Add(structType);
			hiderType.Fields.Add(dataConstantField);
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
			hiderType.Methods.Add(staticConstructor);
			staticConstructor.Body = new MethodBody(staticConstructor);
			staticConstructor.Body.Variables.Add(new VariableDefinition(assembly.TypeSystem.Int32));

			#region CIL of static constructor
			var processor = staticConstructor.Body.GetILProcessor();
			processor.Emit(OpCodes.Newobj, assembly.Import(typeof(Dictionary<double, double>).GetConstructor(new System.Type[0])));
			processor.Emit(OpCodes.Stsfld, doubleDictField);
			processor.Emit(OpCodes.Newobj, assembly.Import(typeof(Dictionary<long, long>).GetConstructor(new System.Type[0])));
			processor.Emit(OpCodes.Stsfld, longDictField);
			processor.Emit(OpCodes.Newobj, assembly.Import(typeof(Dictionary<float, float>).GetConstructor(new System.Type[0])));
			processor.Emit(OpCodes.Stsfld, floatDictField);
			processor.Emit(OpCodes.Newobj, assembly.Import(typeof(Dictionary<int, int>).GetConstructor(new System.Type[0])));
			processor.Emit(OpCodes.Stsfld, intDictField);
			processor.Emit(OpCodes.Newobj, assembly.Import(typeof(Dictionary<byte, byte>).GetConstructor(new System.Type[0])));
			processor.Emit(OpCodes.Stsfld, byteDictField);
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
			#endregion

			assembly.AddType(hiderType);
		}

		public void HideStrings(IEnumerable<StringInstruction> stringInstructions)
		{
			var nameInterator = "";
			var stringIndex = 0;
			foreach (var instruction in stringInstructions)
			{
				if (!_methodByString.TryGetValue(instruction.String, out MethodDefinition individualStringMethodDefinition))
				{
					var methodName = iterator.Next();

					var start = _dataBytes.Count;
					_dataBytes.AddRange(Encoding.UTF8.GetBytes(instruction.String));
					var count = _dataBytes.Count - start;

					individualStringMethodDefinition = CreateIndividualStringMethod(methodName, stringIndex, start, count);
					hiderType.Methods.Add(individualStringMethodDefinition);
					_methodByString.Add(instruction.String, individualStringMethodDefinition);

					stringIndex++;
				}

				var newinstruction = Instruction.Create(OpCodes.Call, individualStringMethodDefinition);
				instruction.ReplaceStringWithInstruction(newinstruction);
			}
			this.stringIndex = stringIndex;
		}

		public void HideDoubles(IEnumerable<NumberInstruction<double>> doubleInstructions)
		{
			foreach (var instruction in doubleInstructions)
			{
				var initializeConst = Instruction.Create(OpCodes.Ldc_R8, ReverseDouble(instruction.Number));
				var callMethodInstruction = Instruction.Create(OpCodes.Call, baseGetDouble);
				instruction.ReplaceNumberWithInstruction(initializeConst, callMethodInstruction);
			}
		}

		public void HideLongs(IEnumerable<NumberInstruction<long>> longInstructions)
		{
			foreach (var instruction in longInstructions)
			{
				var initializeConst = Instruction.Create(OpCodes.Ldc_I8, ReverseLong(instruction.Number));
				var callMethodInstruction = Instruction.Create(OpCodes.Call, baseGetLong);
				instruction.ReplaceNumberWithInstruction(initializeConst, callMethodInstruction);
			}
		}

		public void HideFloats(IEnumerable<NumberInstruction<float>> floatsInstructions)
		{
			foreach (var instruction in floatsInstructions)
			{
				var initializeConst = Instruction.Create(OpCodes.Ldc_R4, ReverseFloat(instruction.Number));
				var callMethodInstruction = Instruction.Create(OpCodes.Call, baseGetFloat);
				instruction.ReplaceNumberWithInstruction(initializeConst, callMethodInstruction);
			}
		}

		public void HideInts(IEnumerable<NumberInstruction<int>> intInstructions)
		{
			foreach (var instruction in intInstructions)
			{
				var initializeConst = Instruction.Create(OpCodes.Ldc_I4, ReverseInt(instruction.Number));
				var callMethodInstruction = Instruction.Create(OpCodes.Call, baseGetInt);
				instruction.ReplaceNumberWithInstruction(initializeConst, callMethodInstruction);
			}
		}

		public void HideBytes(IEnumerable<NumberInstruction<sbyte>> byteInstructions)
		{
			foreach (var instruction in byteInstructions)
			{
				var initializeConst = Instruction.Create(OpCodes.Ldc_I4_S, (sbyte) ReverseByte((byte) instruction.Number));
				var callMethodInstruction = Instruction.Create(OpCodes.Call, baseGetByte);
				instruction.ReplaceNumberWithInstruction(initializeConst, callMethodInstruction);
			}
		}

		public static double ReverseDouble(double d)
		{
			return BitConverter.Int64BitsToDouble(ReverseLong(BitConverter.DoubleToInt64Bits(d)));
		}

		public static float ReverseFloat(float d)
		{
			return BitConverter.ToSingle(BitConverter.GetBytes(ReverseInt(BitConverter.ToInt32(BitConverter.GetBytes(d), 0))), 0);
		}

		public static long ReverseLong(long d)
		{
			var v = (ulong)~d;
			var r = v;
			int s = 63;
			for (v >>= 1; v != 0; v >>= 1)
			{
				r <<= 1;
				r |= (v & 1);
				s--;
			}
			r <<= s;

			return (long)r;
		}

		public static int ReverseInt(int d)
		{
			var v = (uint)~d;
			var r = v;
			int s = 31;
			for (v >>= 1; v != 0; v >>= 1)
			{
				r <<= 1;
				r |= (v & 1);
				s--;
			}
			r <<= s;

			return (int)r;
		}

		public static byte ReverseByte(byte d)
		{
			var v = (byte)~d;
			var r = v;
			int s = 7;
			for (v >>= 1; v != 0; v >>= 1)
			{
				r <<= 1;
				r |= (byte)(v & 1);
				s--;
			}
			r <<= s;

			return (byte)r;
		}
	}
}
