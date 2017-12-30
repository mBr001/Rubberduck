using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Linq;
using System.Threading;
using Rubberduck.Parsing.VBA;
using Rubberduck.Settings;
using Rubberduck.UI.ToDoItems;
using RubberduckTests.Mocks;
using Rubberduck.Common;
using Rubberduck.VBEditor.SafeComWrappers;

namespace RubberduckTests.TodoExplorer
{
    [TestClass]
    public class TodoExplorerTests
    {
        [TestMethod]
        [TestCategory("Annotations")]
        public void PicksUpComments()
        {
            const string inputCode =
                @"' Todo this is a todo comment
' Note this is a note comment
' Bug this is a bug comment
";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("TestProject1", ProjectProtection.Unprotected)
                .AddComponent("Module1", ComponentType.StandardModule, inputCode)
                .Build();

            var vbe = builder.AddProject(project).Build();
            var parser = MockParser.Create(vbe.Object);
            using (var state = parser.State)
            {
                var cs = GetConfigService(new[] { "TODO", "NOTE", "BUG" });
                var vm = new ToDoExplorerViewModel(state, cs, GetOperatingSystemMock().Object);

                parser.Parse(new CancellationTokenSource());
                if (state.Status >= ParserState.Error)
                {
                    Assert.Inconclusive("Parser Error");
                }

                var comments = vm.Items.Select(s => s.Type);

                Assert.IsTrue(comments.SequenceEqual(new[] { "TODO", "NOTE", "BUG" }));
            }
        }

        [TestMethod]
        [TestCategory("Annotations")]
        public void PicksUpComments_StrangeCasing()
        {
            const string inputCode =
                @"' tODO this is a todo comment
' NOTE  this is a note comment
' bug this is a bug comment
' bUg this is a bug comment
";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("TestProject1", ProjectProtection.Unprotected)
                .AddComponent("Module1", ComponentType.StandardModule, inputCode)
                .Build();

            var vbe = builder.AddProject(project).Build();
            var parser = MockParser.Create(vbe.Object);
            using (var state = parser.State)
            {
                var cs = GetConfigService(new[] { "TODO", "NOTE", "BUG" });
                var vm = new ToDoExplorerViewModel(state, cs, GetOperatingSystemMock().Object);

                parser.Parse(new CancellationTokenSource());
                if (state.Status >= ParserState.Error)
                {
                    Assert.Inconclusive("Parser Error");
                }

                var comments = vm.Items.Select(s => s.Type);

                Assert.IsTrue(comments.SequenceEqual(new[] { "TODO", "NOTE", "BUG", "BUG" }));
            }
        }

        [TestMethod]
        [TestCategory("Annotations")]
        public void PicksUpComments_SpecialCharacters()
        {
            const string inputCode =
                @"' To-do - this is a todo comment
' N@TE this is a note comment
' bug this should work with a trailing space
' bug: this should not be seen due to the colon
";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("TestProject1", ProjectProtection.Unprotected)
                .AddComponent("Module1", ComponentType.StandardModule, inputCode)
                .Build();

            var vbe = builder.AddProject(project).Build();
            var parser = MockParser.Create(vbe.Object);
            using (var state = parser.State)
            {
                var cs = GetConfigService(new[] { "TO-DO", "N@TE", "BUG " });
                var vm = new ToDoExplorerViewModel(state, cs, GetOperatingSystemMock().Object);

                parser.Parse(new CancellationTokenSource());
                if (state.Status >= ParserState.Error)
                {
                    Assert.Inconclusive("Parser Error");
                }

                var comments = vm.Items.Select(s => s.Type);

                Assert.IsTrue(comments.SequenceEqual(new[] { "TO-DO", "N@TE", "BUG " }));
            }
        }

        [TestMethod]
        [TestCategory("Annotations")]
        public void AvoidsFalsePositiveComments()
        {
            const string inputCode =
                @"' Todon't should not get picked up
' Debug.print() would trigger false positive if word boundaries not used
' Denoted 
";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("TestProject1", ProjectProtection.Unprotected)
                .AddComponent("Module1", ComponentType.StandardModule, inputCode)
                .Build();

            var vbe = builder.AddProject(project).Build();
            var parser = MockParser.Create(vbe.Object);
            using (var state = parser.State)
            {
                var cs = GetConfigService(new[] { "TODO", "NOTE", "BUG" });
                var vm = new ToDoExplorerViewModel(state, cs, GetOperatingSystemMock().Object);

                parser.Parse(new CancellationTokenSource());
                if (state.Status >= ParserState.Error)
                {
                    Assert.Inconclusive("Parser Error");
                }

                var comments = vm.Items.Select(s => s.Type);

                Assert.IsTrue(comments.Count() == 0);
            }
        }

        [TestMethod]
        [TestCategory("Annotations")]
        public void RemoveRemovesComment()
        {
            const string inputCode =
                @"Dim d As Variant  ' bug should be Integer";

            const string expected =
                @"Dim d As Variant  ";

            var builder = new MockVbeBuilder();
            var project = builder.ProjectBuilder("TestProject1", ProjectProtection.Unprotected)
                .AddComponent("Module1", ComponentType.StandardModule, inputCode)
                .Build();

            var vbe = builder.AddProject(project).Build();
            var parser = MockParser.Create(vbe.Object);
            using (var state = parser.State)
            {
                var cs = GetConfigService(new[] { "TODO", "NOTE", "BUG" });
                var vm = new ToDoExplorerViewModel(state, cs, GetOperatingSystemMock().Object);

                parser.Parse(new CancellationTokenSource());
                if (state.Status >= ParserState.Error)
                {
                    Assert.Inconclusive("Parser Error");
                }

                vm.SelectedItem = vm.Items.Single();
                vm.RemoveCommand.Execute(null);

                var module = project.Object.VBComponents[0].CodeModule;
                Assert.AreEqual(expected, module.Content());
                Assert.IsFalse(vm.Items.Any());
            }
        }

        private IGeneralConfigService GetConfigService(string[] markers)
        {
            var configService = new Mock<IGeneralConfigService>();
            configService.Setup(c => c.LoadConfiguration()).Returns(GetTodoConfig(markers));

            return configService.Object;
        }

        private Configuration GetTodoConfig(string[] markers)
        {
            var todoSettings = new ToDoListSettings
            {
                ToDoMarkers = markers.Select(m => new ToDoMarker(m)).ToArray()
            };

            var userSettings = new UserSettings(null, null, todoSettings, null, null, null, null);
            return new Configuration(userSettings);
        }

        private Mock<IOperatingSystem> GetOperatingSystemMock()
        {
            return new Mock<IOperatingSystem>();
        }
    }
}
