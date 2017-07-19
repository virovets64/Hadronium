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
        private static string Read(XElement node, string attrName, string defValue = null)
        {
            var attr = node.Attribute(attrName);
            if (attr == null)
                return defValue;
            return attr.Value;
        }

        private static void Write(XElement node, string name, string value, string defValue = null)
        {
            if (value != defValue)
                node.Add(new XAttribute(name, value));
        }

        private static double StringToDouble(string s)
        {
            return double.Parse(s, CultureInfo.InvariantCulture);
        }

        private static string DoubleToString(double x)
        {
            return x.ToString(CultureInfo.InvariantCulture);
        }

        private static String[] CoordNames = new String[] { "X", "Y", "Z" };

        private static void Read(XElement node, ref double[] result)
        {
            if (node == null)
                return;
            for (int i = 0; i < result.Length; i++)
                result[i] = StringToDouble(Read(node, CoordNames[i]));
        }

        private static void Write(XElement node, string name, double[] value)
        {
            var newElement = new XElement(name);
            for (int i = 0; i < value.Length; i++)
                Write(newElement, CoordNames[i], DoubleToString(value[i]));
            node.Add(newElement);
        }

        private static void Read(XElement node, ref Color result)
        {
            if (node == null)
                return;
            result.R = byte.Parse(Read(node, "R"));
            result.G = byte.Parse(Read(node, "G"));
            result.B = byte.Parse(Read(node, "B"));
            result.A = byte.Parse(Read(node, "A", "255"));
        }

        private static void Write(XElement node, string name, Color value)
        {
            var newElement = new XElement(name);
            Write(newElement, "R", value.R.ToString());
            Write(newElement, "G", value.G.ToString());
            Write(newElement, "B", value.B.ToString());
            Write(newElement, "A", value.A.ToString(), "255");
            node.Add(newElement);
        }

        private static Particle ReadParticle(XElement node, int dimension)
        {
            var result = new Particle(dimension);
            result.Name = Read(node, "Id");
            result.Mass = StringToDouble(Read(node, "Mass", "1"));
            Read(node.Element("Position"), ref result.Position);
            Read(node.Element("Velocity"), ref result.Velocity);
            Read(node.Element("FillColor"), ref result.FillColor);
            Read(node.Element("StrokeColor"), ref result.StrokeColor);
            return result;
        }

        private static void Write(XElement node, string name, Particle value)
        {
            var newElement = new XElement(name);
            Write(newElement, "Id", value.Name, null);
            Write(newElement, "Mass", DoubleToString(value.Mass), "1");
            Write(newElement, "Position", value.Position);
            Write(newElement, "Velocity", value.Velocity);
            Write(newElement, "FillColor", value.FillColor);
            Write(newElement, "StrokeColor", value.StrokeColor);
            node.Add(newElement);
        }

        private static Particle ReadParticleRef(XElement node, string name, Model model)
        {
            var attr = node.Attribute(name + ".Name");
            if (attr != null)
                return model.GetParticle(attr.Value);
            attr = node.Attribute(name + ".Number");
            if (attr != null)
                return model.Particles[int.Parse(attr.Value)];
            throw new Exception("Link read error");
        }

        private static Link ReadLink(XElement node, Model model)
        {
            var link = new Link(
              ReadParticleRef(node, "A", model),
              ReadParticleRef(node, "B", model),
              double.Parse(Read(node, "Strength", "1")));
            return link;
        }

        private static void WriteParticleRef(XElement node, string name, Model model, Particle particle)
        {
            if (!String.IsNullOrEmpty(particle.Name))
                Write(node, name + ".Name", particle.Name);
            else
                Write(node, name + ".Number", model.GetParticleIndex(particle).ToString());
        }

        private static void Write(XElement node, string name, Link link, Model model)
        {
            var linkNode = new XElement(name);
            WriteParticleRef(linkNode, "A", model, link.A);
            WriteParticleRef(linkNode, "B", model, link.B);
            Write(linkNode, "Strength", link.Strength.ToString(), "1");
            node.Add(linkNode);
        }

        private static void Read(XElement node, PropertyInstance prop)
        {
            var propNode = node.Elements("Property").FirstOrDefault(x => x.Attribute("Name").Value == prop.Description.Name);
            if (propNode != null)
                prop.Description.SetValue(prop.Target, StringToDouble(propNode.Attribute("Value").Value));
        }

        private static void Write(XElement node, PropertyInstance prop)
        {
            var propNode = new XElement("Property");
            Write(propNode, "Name", prop.Description.Name);
            Write(propNode, "Value", DoubleToString((double)prop.Description.GetValue(prop.Target)));
            node.Add(propNode);
        }

        public static Model ReadModel(string fileName, IEnumerable<PropertyInstance> properties)
        {
            var doc = XDocument.Load(fileName);
            var rootNode = doc.Root;
            int dimension = int.Parse(Read(rootNode, "Dimension", "2"));

            var model = new Model(dimension);

            foreach (var prop in properties)
                Read(rootNode, prop);

            foreach (var particleNode in rootNode.Elements("Particle"))
                model.AddParticle(ReadParticle(particleNode, model.Dimension));

            foreach (var linkNode in rootNode.Elements("Link"))
                model.AddLink(ReadLink(linkNode, model));

            return model;
        }


        public static void WriteModel(string fileName, Model model, IEnumerable<PropertyInstance> properties)
        {
            var doc = new XDocument(
              new XElement("Hadronium",
                new XAttribute("Dimension", model.Dimension)));

            foreach (var prop in properties)
                Write(doc.Root, prop);

            foreach (var p in model.Particles)
                Write(doc.Root, "Particle", p);

            foreach (var link in model.Links)
                Write(doc.Root, "Link", link, model);

            doc.Save(fileName);
        }

    }
}
