using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CodeToFlowgraph
{
    public class Node
    {
        internal static readonly string nodeBaseFormat = @"
    <node id=""{0}"">
      <data key=""d6"">
        <y:GenericNode configuration = ""com.yworks.flowchart.{1}"">
          <y:NodeLabel>{2}</y:NodeLabel>
        </y:GenericNode>
      </data>
    </node>";
        static int lastId;

        int id;
        public string ID
        {
            get
            {
                return "n" + id.ToString();
            }
        }

        public string Label { get; set; }
        public Node LinkNode { get; set; }

        public Node()
        {
            id = lastId;
            lastId++;
        }
    }

    public class ProcessNode : Node
    {
        public ProcessNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "process", Label);
        }
    }

    public class ConditionNode : Node
    {
        public Node YesNode { get; set; }

        public ConditionNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "decision", Label);
        }
    }

    public class ForNode : Node
    {
        public Node BodyNode { get; set; }

        public ForNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "preparation", Label);
        }
    }

    public class ForEachNode : Node
    {
        public Node BodyNode { get; set; }

        public ForEachNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "preparation", Label);
        }
    }

    public class StartNode : Node
    {
        public StartNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "start1", Label);
        }
    }

    public class ReturnNode : Node
    {
        public ReturnNode() : base()
        {

        }

        public override string ToString()
        {
            return string.Format(nodeBaseFormat, ID, "terminator", Label);
        }
    }

    public class GraphMLSerializer
    {
        static string header = @"<?xml version=""1.0"" encoding=""UTF-8"" standalone=""no""?>
<graphml xmlns=""http://graphml.graphdrawing.org/xmlns"" xmlns:java=""http://www.yworks.com/xml/yfiles-common/1.0/java"" xmlns:sys=""http://www.yworks.com/xml/yfiles-common/markup/primitives/2.0"" xmlns:x=""http://www.yworks.com/xml/yfiles-common/markup/2.0"" xmlns:xsi=""http://www.w3.org/2001/XMLSchema-instance"" xmlns:y=""http://www.yworks.com/xml/graphml"" xmlns:yed=""http://www.yworks.com/xml/yed/3"" xsi:schemaLocation=""http://graphml.graphdrawing.org/xmlns http://www.yworks.com/xml/schema/graphml/1.1/ygraphml.xsd"">
  <!--Created by yEd 3.15.0.2-->
  <key attr.name=""Description"" attr.type=""string"" for=""graph"" id=""d0""/>
  <key for=""port"" id=""d1"" yfiles.type=""portgraphics""/>
  <key for=""port"" id=""d2"" yfiles.type=""portgeometry""/>
  <key for=""port"" id=""d3"" yfiles.type=""portuserdata""/>
  <key attr.name=""url"" attr.type=""string"" for=""node"" id=""d4""/>
  <key attr.name=""description"" attr.type=""string"" for=""node"" id=""d5""/>
  <key for=""node"" id=""d6"" yfiles.type=""nodegraphics""/>
  <key for=""graphml"" id=""d7"" yfiles.type=""resources""/>
  <key attr.name=""url"" attr.type=""string"" for=""edge"" id=""d8""/>
  <key attr.name=""description"" attr.type=""string"" for=""edge"" id=""d9""/>
  <key for=""edge"" id=""d10"" yfiles.type=""edgegraphics""/>
  <graph edgedefault=""directed"" id=""G"">
  <data key=""d0""/>
";

        static string footer = @"
</graph>
  <data key=""d7"">
    <y:Resources/>
  </data>
</graphml>";

        static string edge = @"
    <edge id=""{0}"" source=""{1}"" target=""{2}"">
      <data key=""d10"">
        <y:PolyLineEdge>
          <y:Arrows source=""none"" target=""standard""/>
          <y:EdgeLabel>{3}</y:EdgeLabel>
        </y:PolyLineEdge>
      </data>
    </edge>";

        public static string Serialize(List<Node> nodes)
        {
            int edgeId = 0;

            var result = header;

            foreach(var node in nodes)
            {
                result += node.ToString();

                if(node.LinkNode != null)
                {
                    if(node is ConditionNode)
                    {
                        result += string.Format(edge, "e" + edgeId.ToString(), node.ID, (node as ConditionNode).YesNode.ID, "Yes");
                        edgeId++;
                        result += string.Format(edge, "e" + edgeId.ToString(), node.ID, (node as ConditionNode).LinkNode.ID, "No");
                    }
                    else if(node is ForNode)
                    {
                        result += string.Format(edge, "e" + edgeId.ToString(), node.ID, (node as ForNode).BodyNode.ID, "");
                        edgeId++;
                        result += string.Format(edge, "e" + edgeId.ToString(), node.ID, (node as ForNode).LinkNode.ID, "");
                    }
                    else
                    {
                        result += string.Format(edge, "e" + edgeId.ToString(), node.ID, node.LinkNode.ID, "");
                    }
                }

                edgeId++;
            }

            result += footer;

            return result;
        }
    }
}
