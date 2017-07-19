using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml;

namespace Hadronium
{
  class XmlSerializer
  {
    private static string read(XmlNode node, string attrName, string defValue = null)
    {
      var attr = node.Attributes[attrName];
      if (attr == null)
        return defValue;
      return attr.Value;
    }

    private static XmlNode write(XmlNode node, string name, string value, string defValue = null)
    {
      if (value == defValue)
        return null;
      var result = node.OwnerDocument.CreateAttribute(name);
      result.Value = value;
      return node.Attributes.Append(result);
    }

    private static double stringToDouble(string s)
    {
      return double.Parse(s, CultureInfo.InvariantCulture);
    }

    private static string doubleToString(double x)
    {
      return x.ToString(CultureInfo.InvariantCulture);
    }

    private static String[] CoordNames = new String[] { "X", "Y", "Z" };

    private static void read(XmlNode node, ref double[] result)
    {
      if (node == null)
        return;
      for (int i = 0; i < result.Length; i++)
        result[i] = stringToDouble(read(node, CoordNames[i]));
    }

    private static XmlNode write(XmlNode node, string name, double[] value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      for (int i = 0; i < value.Length; i++)
        write(result, CoordNames[i], doubleToString(value[i]));
      return result;
    }

    private static void read(XmlNode node, ref Color result)
    {
      if (node == null)
        return;
      result.R = byte.Parse(read(node, "R"));
      result.G = byte.Parse(read(node, "G"));
      result.B = byte.Parse(read(node, "B"));
      result.A = byte.Parse(read(node, "A", "255"));
    }

    private static XmlNode write(XmlNode node, string name, Color value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "R", value.R.ToString());
      write(result, "G", value.G.ToString());
      write(result, "B", value.B.ToString());
      write(result, "A", value.A.ToString(), "255");
      return result;
    }

    private static Particle readParticle(XmlNode node, int dimension)
    {
      var result = new Particle(dimension);
      result.Name = read(node, "Id");
      result.Mass = stringToDouble(read(node, "Mass", "1"));
      read(node.SelectSingleNode("Position"), ref result.Position);
      read(node.SelectSingleNode("Velocity"), ref result.Velocity);
      read(node.SelectSingleNode("FillColor"), ref result.FillColor);
      read(node.SelectSingleNode("StrokeColor"), ref result.StrokeColor);
      return result;
    }
    
    private static Particle readParticleRef(XmlNode node, string name, Model model)
    {
      var attr = node.Attributes[name + ".Name"];
      if (attr != null)
        return model.GetParticle(attr.Value);
      attr = node.Attributes[name + ".Number"];
      if (attr != null)
        return model.Particles[int.Parse(attr.Value)];
      throw new Exception("Link read error");
    }

    private static XmlNode write(XmlNode node, string name, Particle value)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement(name));
      write(result, "Id", value.Name, null);
      write(result, "Mass", doubleToString(value.Mass), "1");
      write(result, "Position", value.Position);
      write(result, "Velocity", value.Velocity);
      write(result, "FillColor", value.FillColor);
      write(result, "StrokeColor", value.StrokeColor);
      return result;
    }

    private static void writeParticleRef(XmlNode node, string name, Model model, Particle particle)
    {
      if (!String.IsNullOrEmpty(particle.Name))
        write(node, name + ".Name", particle.Name);
      else
        write(node, name + ".Number", model.GetParticleIndex(particle).ToString());
    }

    private static void read(XmlNode node, PropertyDescription prop, object target)
    {
      XmlNode propNode = node.SelectSingleNode(string.Format("Property[@Name='{0}']", prop.Name));
      if (propNode != null)
        prop.SetValue(target, stringToDouble(propNode.Attributes["Value"].Value));
    }

    private static XmlNode write(XmlNode node, PropertyDescription prop, object target)
    {
      var result = node.AppendChild(node.OwnerDocument.CreateElement("Property"));
      write(result, "Name", prop.Name);
      write(result, "Value", doubleToString((double)prop.GetValue(target)));
      return result;
    }

    public static Model readModel(string fileName, IEnumerable<PropertyInstance> properties)
    {
      var doc = new XmlDocument();
      doc.Load(fileName);
      var rootNode = doc.DocumentElement;
      int dimension = int.Parse(read(rootNode, "Dimension", "2"));

      var model = new Model(dimension);
      foreach (XmlNode particleNode in rootNode.SelectNodes("Particle"))
      {
        model.Particles.Add(readParticle(particleNode, model.Dimension));
      }
      foreach (var prop in properties)
        read(rootNode, prop.description, prop.target);

      foreach (XmlNode linkNode in rootNode.SelectNodes("Link"))
      {
        var link = new Link(
          readParticleRef(linkNode, "A", model),
          readParticleRef(linkNode, "B", model),
          double.Parse(read(linkNode, "Strength", "1")));
        model.Links.Add(link);
      }
      return model;
    }

    public static void writeModel(string fileName, Model model, IEnumerable<PropertyInstance> properties)
    {
      var doc = new XmlDocument();
      doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
      var rootNode = doc.AppendChild(doc.CreateElement("Hadronium"));
      write(rootNode, "Dimension", model.Dimension.ToString());
      foreach (var prop in properties)
        write(rootNode, prop.description, prop.target);

      foreach (var p in model.Particles)
        write(rootNode, "Particle", p);
      foreach (var l in model.Links)
      {
        var linkNode = rootNode.AppendChild(doc.CreateElement("Link"));
        writeParticleRef(linkNode, "A", model, l.A);
        writeParticleRef(linkNode, "B", model, l.B);
        write(linkNode, "Strength", l.Strength.ToString(), "1");
      }
      doc.Save(fileName);
    }
  }
}
