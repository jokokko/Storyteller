﻿using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using Storyteller.Core.Grammars;
using Storyteller.Core.Grammars.Paragraphs;
using Storyteller.Core.Model;

namespace Storyteller.Core.Testing.Grammars.Paragraphs
{
    [TestFixture]
    public class ParagraphGrammarTester
    {
        [Test]
        public void do_adds_a_silent_grammar_at_the_right_position()
        {
            var paragraph = new ParagraphGrammar("Something");
            paragraph.Children.Any().ShouldBeFalse();

            paragraph.Do(c => {});

            paragraph.Children[0].ShouldBeOfType<SilentGrammar>()
                .Position.ShouldEqual(0);


            paragraph.Do(c => { });
            paragraph.Do(c => { });
            paragraph.Do(c => { });

            paragraph.Children[1].ShouldBeOfType<SilentGrammar>().Position.ShouldEqual(1);
            paragraph.Children[2].ShouldBeOfType<SilentGrammar>().Position.ShouldEqual(2);
            paragraph.Children[3].ShouldBeOfType<SilentGrammar>().Position.ShouldEqual(3);

        }

        [Test]
        public void surfaces_all_the_child_errors()
        {
            var child1 = new Sentence();
            child1.errors.Add(new GrammarError());
            child1.errors.Add(new GrammarError());

            var child2 = new Sentence();
            child2.errors.Add(new GrammarError());
            child2.errors.Add(new GrammarError());

            var paragraph = new Paragraph(new GrammarModel[] {child1, child2});
            paragraph.errors.Count.ShouldEqual(4);
        }
    }
}