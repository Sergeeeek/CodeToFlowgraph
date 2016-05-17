using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeToFlowgraph
{
    class Program
    {
        static void Main(string[] args)
        {
            var workspace = MSBuildWorkspace.Create();

            var solution = workspace.OpenSolutionAsync(args[0]).Result;

            var proj = solution.Projects.First();

            foreach(var doc in proj.Documents)
            {
                Console.WriteLine(doc.Name);

                var model = doc.GetSemanticModelAsync().Result;

                

                var methodSyntax = model.SyntaxTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
                

                foreach(var meth in methodSyntax)
                {
                    var spans = ProcessBlock(meth.Body).Distinct();
                    var nds = GraphMLSerializer.Serialize(PostProcess(ProcessNodes(meth.Body)));

                    foreach(var span in spans)
                    {
                        Console.WriteLine(meth.SyntaxTree.GetText().GetSubText(span));
                    }

                    var methSym = model.GetDeclaredSymbol(meth);
                    var cf = model.AnalyzeControlFlow(meth.Body);

                    var ifs = meth.Body.DescendantNodes().OfType<IfStatementSyntax>();
                    //var ancestors = ifs.Count() == 0 ? null : ifs.First().;

                    var name = methSym.Name;
                    Console.WriteLine(name);

                    var param = methSym.Parameters;

                    foreach(var par in param)
                    {
                        Console.WriteLine(par.Name);
                    }
                }
            }

            Console.ReadLine();
        }

        static List<TextSpan> ProcessBlock(CSharpSyntaxNode block)
        {
            List<TextSpan> spans = new List<TextSpan>();

            foreach (var node in block.ChildNodes())
            {
                if (node.Kind() == SyntaxKind.IfStatement)
                {
                    var ifStatement = node as IfStatementSyntax;
                    spans.Add(ifStatement.Condition.Span);

                    spans.AddRange(ProcessBlock(ifStatement.Statement));

                    if (ifStatement.Else != null)
                    {
                        spans.AddRange(ProcessBlock(ifStatement.Else.Statement));
                    }
                }
                else if (node.Kind() == SyntaxKind.ForStatement)
                {
                    var forStatement = node as ForStatementSyntax;

                    spans.Add(forStatement.Declaration.Span);

                    spans.AddRange(ProcessBlock(forStatement.Statement));
                }
                else if (node.Kind() == SyntaxKind.ForEachStatement)
                {

                }
                else
                {
                    spans.Add(node.Span);
                }
            }

            return spans;
        }

        static List<Node> ProcessNodes(CSharpSyntaxNode block)
        {
            var res = new List<Node>();

            List<TextSpan> spans = new List<TextSpan>();

            Node lastNode = null;
            Node lastIfNode = null;

            List<Node> innerNodes = new List<Node>();

            var text = block.SyntaxTree.GetText();

            foreach(var syntaxNode in block.ChildNodes())
            {
                if(syntaxNode.Kind() == SyntaxKind.IfStatement || syntaxNode.Kind() == SyntaxKind.ForStatement || syntaxNode.Kind() == SyntaxKind.ForEachStatement)
                {
                    if (spans.Count > 0)
                    {
                        spans = spans.Distinct().ToList();

                        var process = string.Concat(spans.Select(x => text.GetSubText(x).ToString() + "\n"));

                        spans.Clear();

                        var pNode = new ProcessNode() { Label = process };

                        if (lastNode != null)
                        {
                            lastNode.LinkNode = pNode;
                        }

                        if (lastIfNode != null)
                        {
                            lastIfNode.LinkNode = pNode;
                            lastIfNode = null;
                        }

                        lastNode = pNode;
                        res.Add(lastNode);
                    }

                    if(syntaxNode.Kind() == SyntaxKind.IfStatement)
                    {
                        var ifNode = syntaxNode as IfStatementSyntax;
                        
                        var ifs = new ConditionNode() { Label = text.GetSubText(ifNode.Condition.Span).ToString() };

                        if(lastNode != null)
                        {
                            lastNode.LinkNode = ifs;
                        }

                        if (lastIfNode != null)
                        {
                            lastIfNode.LinkNode = ifs;
                        }

                        for(int i = innerNodes.Count - 1; i >= 0; i--)
                        {
                            innerNodes[i].LinkNode = ifs;
                            innerNodes.RemoveAt(i);
                        }

                        lastIfNode = ifs;
                        lastNode = ifs;

                        res.Add(lastNode);

                        var cnodes = ProcessNodes(ifNode.Statement);

                        ifs.YesNode = cnodes.First();

                        res.AddRange(cnodes);
                        lastNode = cnodes.Last();

                        foreach(var cnode in cnodes)
                        {
                            if(cnode.LinkNode == null)
                            {
                                innerNodes.Add(cnode);
                            }
                        }

                        if(ifNode.Else != null)
                        {
                            var elseNodes = ProcessNodes(ifNode.Else.Statement);

                            lastIfNode.LinkNode = elseNodes.First();
                            lastIfNode = null;

                            res.AddRange(elseNodes);

                            lastNode = elseNodes.Last();

                            foreach (var enode in elseNodes)
                            {
                                if ((enode is ConditionNode || enode is ForNode || enode is ForEachNode) && enode.LinkNode == null)
                                {
                                    innerNodes.Add(enode);
                                }
                            }
                        }
                    }
                    else if(syntaxNode.Kind() == SyntaxKind.ForStatement)
                    {
                        var forNode = syntaxNode as ForStatementSyntax;

                        var fors = new ForNode() { Label = text.GetSubText(forNode.Declaration.Span).ToString() + "; " + text.GetSubText(forNode.Condition.Span) + "; " + text.GetSubText(forNode.Incrementors.Span) };

                        if(lastNode != null)
                        {
                            lastNode.LinkNode = fors;
                        }

                        if(lastIfNode != null)
                        {
                            lastIfNode.LinkNode = fors;
                            lastIfNode = null;
                        }

                        for (int i = innerNodes.Count - 1; i >= 0; i--)
                        {
                            innerNodes[i].LinkNode = fors;
                            innerNodes.RemoveAt(i);
                        }

                        lastNode = fors;

                        var fnodes = ProcessNodes(forNode.Statement);

                        fors.BodyNode = fnodes.First();
                        fnodes.Last().LinkNode = fors;

                        res.Add(lastNode);
                        res.AddRange(fnodes);

                        foreach (var fnode in fnodes)
                        {
                            if (fnode.LinkNode == null)
                            {
                                innerNodes.Add(fnode);
                            }
                        }
                    }
                }
                else if(syntaxNode.Kind() == SyntaxKind.EmptyStatement)
                {
                    continue;
                }
                else
                {
                    spans.Add(syntaxNode.Span);
                }
            }

            if(spans.Count > 0)
            {
                spans = spans.Distinct().ToList();

                var process = string.Concat(spans.Select(x => text.GetSubText(x).ToString() + "\n"));

                spans.Clear();

                var pNode = new ProcessNode() { Label = process };

                if (lastNode != null)
                {
                    lastNode.LinkNode = pNode;
                }

                if(lastIfNode != null)
                {
                    lastIfNode.LinkNode = pNode;
                    lastIfNode = null;
                }

                for (int i = innerNodes.Count - 1; i >= 0; i--)
                {
                    innerNodes[i].LinkNode = pNode;
                    innerNodes.RemoveAt(i);
                }

                lastNode = pNode;
                res.Add(lastNode);
            }

            if(block.Kind() == SyntaxKind.ReturnStatement)
            {
                res.Add(new ReturnNode());
            }

            return res;
        }

        static List<Node> PostProcess(List<Node> input)
        {
            input.Insert(0, new StartNode() { Label = "начало", LinkNode = input.First() });
            input.Add(new ReturnNode() { Label = "конец" });

            for(int i = input.Count - 1; i >= 0; i--)
            {
                if(input[i] is ConditionNode && (input[i] as ConditionNode).YesNode is ReturnNode && (input[i] as ConditionNode).YesNode != input.Last())
                {
                    (input[i] as ConditionNode).YesNode = input.Last();
                }

                if(input[i].LinkNode is ReturnNode && input[i].LinkNode != input.Last())
                {
                    input[i].LinkNode = input.Last();
                }

                if(input[i] is ReturnNode && input[i] != input.Last())
                {
                    input.RemoveAt(i);
                    continue;
                }

                if(input[i].LinkNode == null && input[i] != input.Last())
                {
                    input[i].LinkNode = input.Last();
                }
            }

            return input;
        }
    }
}
