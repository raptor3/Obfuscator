﻿using Mono.Cecil;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Obfuscator.Structure;
using System.IO;
using Obfuscator.Iterator;

namespace Obfuscator
{
	public class Program
	{
		private static void ShowHelp()
		{
			Console.WriteLine("Usage: obfuscar [projectfile] [outputDirectory]");
		}

		private static int Main(string[] args)
		{
			Console.WriteLine();

			if (args.Length != 2)
			{
				ShowHelp();
				return 1;
			}

			int start = Environment.TickCount;

			try
			{
				Console.Write("Loading project...");

				XmlSchemaSet schemas = new XmlSchemaSet();
				schemas.Add("", "PropertiesSchema.xsd");

				XmlReaderSettings settings = new XmlReaderSettings();
				settings.ValidationType = ValidationType.Schema;
				settings.Schemas = schemas;
				settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

				XmlReader reader = XmlReader.Create(args[0], settings);

				XmlSerializer serializer = new XmlSerializer(typeof(Project));

				var project = (Project)serializer.Deserialize(reader);

				project.Load(new DefaultAssemblyResolver());
				project.Resolve();
				project.RunRules(new NameIterator());

				Directory.CreateDirectory(args[1]);
				project.SaveAssemblies(args[1]);

				Console.WriteLine("Completed, {0:f2} secs.", (Environment.TickCount - start) / 1000.0);
			}
			catch (Exception e)
			{
				Console.WriteLine();
				Console.Error.WriteLine("An error occurred during processing:");
				Console.Error.WriteLine(e.Message);
				if (e.InnerException != null)
					Console.Error.WriteLine(e.InnerException.Message);
				return 1;
			}
			return 0;
		}

		private static void ValidationCallBack(object sender, ValidationEventArgs e)
		{
			throw new Exception(string.Format("Validation Error: {0}", e.Message));
		}
	}
}