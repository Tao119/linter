using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CSharpLinter
{
    class Program
    {
        static void Main(string[] args)
        {
            string code = Console.In.ReadToEnd();

            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            var compilation = CSharpCompilation.Create("MyCompilation",
                syntaxTrees: new[] { tree }, references: new[] { mscorlib });

            var model = compilation.GetSemanticModel(tree);
            var diagnostics = compilation.GetDiagnostics();
            var issues = new List<object>();

            // コンパイルエラーと警告の処理
            foreach (var diagnostic in diagnostics)
            {
                var lineSpan = diagnostic.Location.GetLineSpan().StartLinePosition;
                issues.Add(new
                {
                    severity = diagnostic.Severity.ToString(),
                    message = diagnostic.GetMessage(),
                    line = lineSpan.Line + 1,
                    column = lineSpan.Character + 1
                });
            }

            // 使用されていない変数の検出
            var variableDecls = tree.GetRoot().DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variable in variableDecls)
            {
                var symbol = model.GetDeclaredSymbol(variable);
                var references = model.SyntaxTree.GetRoot().DescendantNodes()
                    .OfType<IdentifierNameSyntax>()
                    .Where(id => SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(id).Symbol, symbol));

                if (!references.Any())
                {
                    var lineSpan = variable.GetLocation().GetLineSpan().StartLinePosition;
                    issues.Add(new
                    {
                        severity = "Warning",
                        message = $"Variable '{variable.Identifier.Text}' is declared but never used.",
                        line = lineSpan.Line + 1,
                        column = lineSpan.Character + 1
                    });
                }
            }

            // マジックナンバーの警告
            var literals = tree.GetRoot().DescendantNodes().OfType<LiteralExpressionSyntax>();
            foreach (var literal in literals.Where(l => l.IsKind(SyntaxKind.NumericLiteralExpression)))
            {
                var lineSpan = literal.GetLocation().GetLineSpan().StartLinePosition;
                issues.Add(new
                {
                    severity = "Info",
                    message = $"Magic number detected: {literal.Token.ValueText}",
                    line = lineSpan.Line + 1,
                    column = lineSpan.Character + 1
                });
            }

            // パブリックメソッドのXMLコメントの確認
            var methods = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                if (method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                    (method.GetLeadingTrivia().All(tr => !tr.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) &&
                                                         !tr.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))))
                {
                    var lineSpan = method.GetLocation().GetLineSpan().StartLinePosition;
                    issues.Add(new
                    {
                        severity = "Warning",
                        message = $"Public method '{method.Identifier.Text}' is missing XML documentation comments.",
                        line = lineSpan.Line + 1,
                        column = lineSpan.Character + 1
                    });
                }
            }

            // クラス名がパスカルケースであるかのチェック
            var classDeclarations = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();
            foreach (var cls in classDeclarations)
            {
                if (!Regex.IsMatch(cls.Identifier.Text, @"^[A-Z][a-zA-Z]*$"))
                {
                    var lineSpan = cls.GetLocation().GetLineSpan().StartLinePosition;
                    issues.Add(new
                    {
                        severity = "Warning",
                        message = $"Class name '{cls.Identifier.Text}' should be in PascalCase.",
                        line = lineSpan.Line + 1,
                        column = lineSpan.Character + 1
                    });
                }
            }

            // JSON形式で結果を出力
            string json = JsonConvert.SerializeObject(issues, Formatting.Indented);
            Console.WriteLine(json);
        }
    }
    public class Issue
    {
        public string? Description { get; set; }
    }
}
