using Mono.Cecil;
using Obfuscator.Iterator;
using Obfuscator.Structure;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Obfuscator
{
	public partial class StartForm : Form
	{
		private Project project;

		public StartForm()
		{
			InitializeComponent();
		}

		private static void ValidationCallBack(object sender, ValidationEventArgs e)
		{
			throw new Exception(string.Format("Validation Error: {0}", e.Message));
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (project != null)
			{
				try
				{
					project.Load(new DefaultAssemblyResolver());
					project.Resolve();
					project.HideStrings();
					var result = project.RunRules();
					project.AddSecurity();


					project.SaveAssemblies();
					File.WriteAllText(project.OutputFolder + @"\result.txt", result);

					richTextBox1.Text = result;
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error: Could not defend application. Original error: " + ex.Message);
				}
			}
		}

		private void button1_Click(object sender, EventArgs e)
		{
			var openDialog = new OpenFileDialog();
			openDialog.InitialDirectory = Directory.GetCurrentDirectory();
			openDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";
			openDialog.FilterIndex = 2;
			openDialog.RestoreDirectory = true;

			if (openDialog.ShowDialog() == DialogResult.OK)
			{
				try
				{
					var schemas = new XmlSchemaSet();
					schemas.Add("", "PropertiesSchema.xsd");

					var settings = new XmlReaderSettings()
					{
						ValidationType = ValidationType.Schema,
						Schemas = schemas
					};
					settings.ValidationEventHandler += new ValidationEventHandler(ValidationCallBack);

					XmlReader reader = XmlReader.Create(openDialog.FileName, settings);

					var serializer = new XmlSerializer(typeof(Project));

					project = (Project)serializer.Deserialize(reader);
					project.NameIteratorFabric = new AlphabetIteratorFabric();

					textBox1.Text = openDialog.FileName;
					richTextBox1.Text = File.ReadAllText(openDialog.FileName);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Error: Could not read file from disk. Original error: " + ex.Message);
				}
			}
		}
	}
}
