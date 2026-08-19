using System;
using HordeForge.Core;
using HordeForge.Core.Data;

namespace HordeForge.Tests
{
    /// <summary>
    /// Baut eine startbereite Simulation mit festem Zufallsseed, damit Tests
    /// reproduzierbar bleiben.
    /// </summary>
    internal static class TestSim
    {
        public static GameSimulation Create(int seed = 20260819, Action<GameConfig> configure = null)
        {
            GameConfig config = DefaultContent.CreateConfig();

            if (configure != null)
            {
                configure(config);
            }

            GameSimulation simulation = new GameSimulation(config, seed);
            simulation.StartNewGame();
            return simulation;
        }

        /// <summary>Laesst die Simulation in realistischen Frames laufen.</summary>
        public static void Run(GameSimulation simulation, float seconds, float step = 0.05f)
        {
            float remaining = seconds;
            while (remaining > 0f)
            {
                float delta = remaining < step ? remaining : step;
                simulation.Tick(delta);
                remaining -= delta;
            }
        }
    }
}
