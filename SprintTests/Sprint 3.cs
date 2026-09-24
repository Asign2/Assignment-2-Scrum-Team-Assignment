using Assign_2;
using System;
using Xunit;

namespace SprintTests
{
    internal class Sprint_3 : IDisposable
    {
        public void Dispose()
        {
            // This automatically runs after EVERY test method finishes.
            // It clears any newly registered users and resets UserDatabase back to default.
            UserDatabase.ResetDatabase();

            // If SensorsDatabase needs a reset or clear as well, you can add it here:
            // SensorsDatabase.ResetDatabase();
        }

    }
}
