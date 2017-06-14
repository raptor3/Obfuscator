using Mono.Cecil;
using System;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Obfuscator.Structure;
using System.IO;
using Obfuscator.Iterator;
using System.Windows.Forms;

namespace Obfuscator
{
	public class Program
	{
		private static void ShowHelp()
		{
			Console.WriteLine("Usage: obfuscar [projectfile] [outputDirectory]");
		}

		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new StartForm());
		}

		private static int m1(string[] args)
		{
			Console.WriteLine();
			var form = new StartForm();
			

			//if (args.Length != 2)
			//{
			//	ShowHelp();
			//	return 1;
			//}

			//try
			//{
			//	Console.Write("Loading project...");

			//	XmlSchemaSet schemas = new XmlSchemaSet();
			//	schemas.Add("", "PropertiesSchema.xsd");

			//	XmlReaderSettings settings = new XmlReaderSettings()
			//	{
			//		ValidationType = ValidationType.Schema,
			//		Schemas = schemas
			//	};
			//	settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

			//	XmlReader reader = XmlReader.Create(args[0], settings);

			//	XmlSerializer serializer = new XmlSerializer(typeof(Project));

			//	var project = (Project)serializer.Deserialize(reader);
			//	project.NameIteratorFabric = new AlphabetIteratorFabric();
			//	project.Load(new DefaultAssemblyResolver());
			//	project.Resolve();
			//	project.HideStrings();
			//	Console.WriteLine();
			//	var result = project.RunRules();
			//	Console.WriteLine(result);
			//	project.AddSecurity();

				
			//	project.SaveAssemblies();
			//	File.WriteAllText(project.OutputFolder + @"\result.txt", result);
			//}
			//finally
			//{
			//	Console.WriteLine();
			//}
			return 0;
		}


	}
}