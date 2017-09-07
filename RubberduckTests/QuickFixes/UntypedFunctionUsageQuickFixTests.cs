﻿using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rubberduck.Inspections.Concrete;
using Rubberduck.Inspections.QuickFixes;
using Rubberduck.Parsing.Annotations;
using Rubberduck.Parsing.Inspections.Resources;
using Rubberduck.Parsing.Symbols;
using Rubberduck.Parsing.VBA;
using Rubberduck.VBEditor;
using Rubberduck.VBEditor.SafeComWrappers;
using RubberduckTests.Mocks;


namespace RubberduckTests.QuickFixes
{
    [TestClass]
    public class UntypedFunctionUsageQuickFixTests
    {
        [TestMethod]
        [TestCategory("QuickFixes")]
        [Ignore] // not sure how to handle GetBuiltInDeclarations
        public void UntypedFunctionUsage_QuickFixWorks()
        {
            const string inputCode =
@"Sub Foo()
    Dim str As String
    str = Left(""test"", 1)
End Sub";

            const string expectedCode =
@"Sub Foo()
    Dim str As String
    str = Left$(""test"", 1)
End Sub";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("VBAProject", ProjectProtection.Unprotected)
                .AddComponent("MyClass", ComponentType.ClassModule, inputCode)
                .AddReference("VBA", MockVbeBuilder.LibraryPathVBA, 4, 1, true)
                .Build();
            var vbe = builder.AddProject(project).Build();

            var component = project.Object.VBComponents[0];
            var parser = MockParser.Create(vbe.Object);

            // FIXME reinstate and unignore tests
            // refers to "UntypedFunctionUsageInspectionTests.GetBuiltInDeclarations()"
            //GetBuiltInDeclarations().ForEach(d => parser.State.AddDeclaration(d));

            parser.Parse(new CancellationTokenSource());
            if (parser.State.Status >= ParserState.Error) { Assert.Inconclusive("Parser Error"); }

            var inspection = new UntypedFunctionUsageInspection(parser.State);
            var inspectionResults = inspection.GetInspectionResults();

            new UntypedFunctionUsageQuickFix(parser.State).Fix(inspectionResults.First());

            Assert.AreEqual(expectedCode, parser.State.GetRewriter(component).GetText());
        }

    }
}
