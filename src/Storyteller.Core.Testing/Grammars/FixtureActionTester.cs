﻿using System;
using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using Rhino.Mocks;
using Storyteller.Core.Grammars;
using Storyteller.Core.Model;
using Storyteller.Core.Results;

namespace Storyteller.Core.Testing.Grammars
{
    [TestFixture]
    public class FixtureActionTester
    {
        [Test]
        public void execute_happy_path()
        {
            var wasCalled = false;
            var action = new FixtureAction(Stage.setup, () => wasCalled = true, new Section("Math"));
            action.Execute(SpecContext.ForTesting());

            wasCalled.ShouldBeTrue();
        }

        [Test]
        public void execute_sad_path()
        {
            var context = SpecContext.ForTesting();
            var ex = new DivideByZeroException();

            var section = new Section("Math") {Id = "5"};
            var action = new FixtureAction(Stage.teardown, () => { throw ex; }, section);

            action.Execute(context);

            var result = context.Results.Single().ShouldBeOfType<StepResult>();

            result.id.ShouldEqual(section.Id);
            result.position.ShouldEqual(Stage.teardown);
            result.status.ShouldEqual(ResultStatus.error);
            result.error.ShouldContain("DivideByZeroException");


        }

        [Test]
        public void exceptions_are_critical()
        {
            var context = SpecContext.ForTesting();
            var ex = new DivideByZeroException();

            var section = new Section("Math") { Id = "5" };
            var action = new FixtureAction(Stage.teardown, () => { throw ex; }, section);

            action.Execute(context);

            context.CanContinue().ShouldBeFalse();
        }

        [Test]
        public void accept_visitor()
        {
            var executor = MockRepository.GenerateMock<ISpecExecutor>();

            var action = new FixtureAction(Stage.setup, () => { }, new Section("Math"));

            action.AcceptVisitor(executor);

            executor.AssertWasCalled(x => x.Action(action));
        }
    }
}