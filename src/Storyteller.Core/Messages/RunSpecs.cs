﻿namespace Storyteller.Core.Messages
{
    public class RunSpecs : ClientMessage
    {
        public RunSpecs()
            : base("run-specs")
        {
        }

        public string[] list;
    }
}