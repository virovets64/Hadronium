using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using System.Xml.Linq;

namespace Hadronium
{
  class XmlSerializer
  {
    private static string read(XElement node, string attrName, string defValue = null)
    {
      var attr = node.Attribute(attrName);
      if (attr == null)
        return defValue;
      return attr.Value;
    }

    private static void write(XElement node, string name, string value, string defValue = null)
    {
      if (value != defValue)
        node.Add(new XAttribute(name, value));
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

    private static void read(XElement node, ref double[] result)
    {
      if (node == null)
        return;
      for (int i = 0; i < result.Length; i++)
        result[i] = stringToDouble(read(node, CoordNames[i]));
    }

    private static void write(XElement node, string name, double[] value)
    {
      var newElement = new XElement(name);
      for (int i = 0; i < value.Length; i++)
        write(newElement, CoordNames[i], doubleToString(value[i]));
      node.Add(newElement);
    }

    private static void read(XElement node, ref Color result)
    {
      if (node == null)
        return;
      result.R = byte.Parse(read(node, "R"));
      result.G = byte.Parse(read(node, "G"));
      result.B = byte.Parse(read(node, "B"));
      result.A = byte.Parse(read(node, "A", "255"));
    }

    private static void write(XElement node, string name, Color value)
    {
      var newElement = new XElement(name);
      write(newElement, "R", value.R.ToString());
      write(newElement, "G", value.G.ToString());
      write(newElement, "B", value.B.ToString());
      write(newElement, "A", value.A.ToString(), "255");
      node.Add(newElement);
    }

    private static Particle readParticle(XElement node, int dimension)
    {
      var result = new Particle(dimension);
      result.Name = read(node, "Id");
      result.Mass = stringToDouble(read(node, "Mass", "1"));
      read(node.Element("Position"), ref result.Position);
      read(node.Element("Velocity"), ref result.Velocity);
      read(node.Element("FillColor"), ref result.FillColor);
      read(node.Element("StrokeColor"), ref result.StrokeColor);
      return result;
    }
    
    private static void write(XElement node, string name, Particle value)
    {
      var newElement = new XElement(name);
      write(newElement, "Id", value.Name, null);
      write(newElement, "Mass", doubleToString(value.Mass), "1");
      write(newElement, "Position", value.Position);
      write(newElement, "Velocity", value.Velocity);
      write(newElement, "FillColor", value.FillColor);
      write(newElement, "StrokeColor", value.StrokeColor);
      node.Add(newElement);
    }

    private static Particle readParticleRef(XElement node, string name, Model model)
    {
      var attr = node.Attribute(name + ".Name");
      if (attr != null)
        return model.GetParticle(attr.Value);
      attr = node.Attribute(name + ".Number");
      if (attr != null)
        return model.Particles[int.Parse(attr.Value)];
      throw new Exception("Link read error");
    }

    private static Link readLink(XElement node, Model model)
    {
      var link = new Link(
        readParticleRef(node, "A", model),
        readParticleRef(node, "B", model),
        double.Parse(read(node, "Strength", "1")));
      return link;
    }

    private static void writeParticleRef(XElement node, string name, Model model, Particle particle)
    {
      if (!String.IsNullOrEmpty(particle.Name))
        write(node, name + ".Name", particle.Name);
      else
        write(node, name + ".Number", model.GetParticleIndex(particle).ToString());
    }

    private static void write(XElement node, string name, Link link, Model model)
    {
      var linkNode = new XElement(name);
      writeParticleRef(linkNode, "A", model, link.A);
      writeParticleRef(linkNode, "B", model, link.B);
      write(linkNode, "Strength", link.Strength.ToString(), "1");
      node.Add(linkNode);
    }

    private static void read(XElement node, PropertyInstance prop)
    {
      var propNode = node.Elements("Property").FirstOrDefault(x => x.Attribute("Name").Value == prop.description.Name);
      if (propNode != null)
        prop.description.SetValue(prop.target, stringToDouble(propNode.Attribute("Value").Value));
    }

    private static void write(XElement node, PropertyInstance prop)
    {
      var propNode = new XElement("Property");
      write(propNode, "Name", prop.description.Name);
      write(propNode, "Value", doubleToString((double)prop.description.GetValue(prop.target)));
      node.Add(propNode);
    }

    public static Model readModel(string fileName, IEnumerable<PropertyInstance> properties)
    {
      var doc = XDocument.Load(fileName);
      var rootNode = doc.Root;
      int dimension = int.Parse(read(rootNode, "Dimension", "2"));

      var model = new Model(dimension);

      foreach (var prop in properties)
        read(rootNode, prop);

      foreach (var particleNode in rootNode.Elements("Particle"))
        model.Particles.Add(readParticle(particleNode, model.Dimension));
      
      foreach (var linkNode in rootNode.Elements("Link"))
        model.Links.Add(readLink(linkNode, model));

      return model;
    }


    public static void writeModel(string fileName, Model model, IEnumerable<PropertyInstance> properties)
    {
      var doc = new XDocument(
        new XElement("Hadronium", 
          new XAttribute("Dimension", model.Dimension)));

      foreach (var prop in properties)
        write(doc.Root, prop);

      foreach (var p in model.Particles)
        write(doc.Root, "Particle", p);

      foreach (var link in model.Links)
        write(doc.Root, "Link", link, model);

      doc.Save(fileName);
    }

  }
}
