using CyborgianStates.Interfaces;
using System;

namespace CyborgianStates.MessageHandling
{
    public class ConsoleInput : IUserInput
    {
        public string GetInput()
        {
            if (Console.KeyAvailable)
            {
                Console.Write("> ");
                return Console.ReadLine().Trim();
            }
            else
            {
                return string.Empty;
            }
        }
    }
}