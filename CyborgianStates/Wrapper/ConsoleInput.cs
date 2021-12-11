using CyborgianStates.Interfaces;
using System;

namespace CyborgianStates.Wrapper
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