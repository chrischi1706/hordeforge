using System;
using System.Collections.Generic;
using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Waves;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class WaveTests
    {
        [Test]
        public void BudgetGrowsWithEveryWave()
        {
            GameConfig config = DefaultContent.CreateConfig();
            HordeGenerator generator = new HordeGenerator(config);

            int previous = 0;
            for (int wave = 1; wave <= 12; wave++)
            {
                int budget = generator.CalculateBudget(wave);
                Assert.Greater(budget, previous, "Welle " + wave + " muss teurer sein als die davor.");
                previous = budget;
            }

            Assert.AreEqual(config.Waves.BaseBudget, generator.CalculateBudget(1));
        }

        [Test]
        public void CompositionNeverExceedsTheBudget()
        {
            GameConfig config = DefaultContent.CreateConfig();
            HordeGenerator generator = new HordeGenerator(config);

            for (int seed = 0; seed < 60; seed++)
            {
                Random random = new Random(seed);
                for (int wave = 1; wave <= 20; wave++)
                {
                    int budget = generator.CalculateBudget(wave);
                    List<HordeEntry> composition = generator.Compose(budget, random);

                    Assert.LessOrEqual(
                        generator.TotalCost(composition), budget,
                        "Welle " + wave + " mit Seed " + seed + " ueberschreitet das Budget.");
                }
            }
        }

        [Test]
        public void EveryWaveProducesAtLeastOneEnemy()
        {
            GameConfig config = DefaultContent.CreateConfig();
            HordeGenerator generator = new HordeGenerator(config);
            Random random = new Random(7);

            for (int wave = 1; wave <= 20; wave++)
            {
                List<HordeEntry> composition =
                    generator.Compose(generator.CalculateBudget(wave), random);
                Assert.Greater(generator.TotalEnemies(composition), 0, "Welle " + wave + " ist leer.");
            }
        }

        [Test]
        public void StrongEnemiesOnlyShowUpOnceTheBudgetIsLargeEnough()
        {
            GameConfig config = DefaultContent.CreateConfig();
            HordeGenerator generator = new HordeGenerator(config);
            Random random = new Random(3);

            // Troll kostet 200, der Freischaltfaktor ist 3 – bei 300 Budget also unmoeglich.
            for (int i = 0; i < 50; i++)
            {
                List<HordeEntry> composition = generator.Compose(300, random);
                for (int e = 0; e < composition.Count; e++)
                {
                    Assert.AreNotEqual(EnemyType.Troll, composition[e].Type);
                }
            }
        }

        [Test]
        public void CompositionVariesBetweenWaves()
        {
            GameConfig config = DefaultContent.CreateConfig();
            HordeGenerator generator = new HordeGenerator(config);
            Random random = new Random(11);

            HashSet<string> shapes = new HashSet<string>();
            for (int i = 0; i < 25; i++)
            {
                List<HordeEntry> composition = generator.Compose(500, random);
                string shape = "";
                for (int e = 0; e < composition.Count; e++)
                {
                    shape += composition[e].Type + ":" + composition[e].Count + ";";
                }

                shapes.Add(shape);
            }

            Assert.Greater(shapes.Count, 1, "Die Zusammensetzung soll sich unterscheiden.");
        }

        [Test]
        public void AZeroEliteChanceNeverProducesAnEliteWave()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.EliteChance = 0f;
                config.Waves.EliteMinWave = 1;
            });

            for (int i = 0; i < 10; i++)
            {
                sim.Waves.StartWaveNow();
                Assert.IsFalse(sim.Waves.IsEliteWave);
                ClearCurrentWave(sim);
            }
        }

        [Test]
        public void AFullEliteChanceAlwaysProducesAnEliteWave()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.EliteChance = 1f;
                config.Waves.EliteMinWave = 1;
            });

            sim.Waves.StartWaveNow();

            Assert.IsTrue(sim.Waves.IsEliteWave);
        }

        [Test]
        public void EarlyWavesAreNeverElite()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.EliteChance = 1f;
                config.Waves.EliteMinWave = 3;
            });

            sim.Waves.StartWaveNow();
            Assert.IsFalse(sim.Waves.IsEliteWave, "Welle 1 liegt unter der Elite-Mindestwelle.");
        }

        [Test]
        public void EliteWavesGetABiggerBudget()
        {
            GameSimulation elite = TestSim.Create(1, config =>
            {
                config.Waves.EliteChance = 1f;
                config.Waves.EliteMinWave = 1;
            });
            elite.Waves.StartWaveNow();

            GameSimulation normal = TestSim.Create(1, config =>
            {
                config.Waves.EliteChance = 0f;
                config.Waves.EliteMinWave = 1;
            });
            normal.Waves.StartWaveNow();

            Assert.Greater(elite.Waves.CurrentBudget, normal.Waves.CurrentBudget);
        }

        [Test]
        public void ClearingAWaveStartsThePreparationForTheNextOne()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.BaseBudget = 10;
                config.Waves.EliteChance = 0f;
            });

            Assert.AreEqual(1, sim.Waves.WaveNumber);
            sim.Waves.StartWaveNow();
            Assert.AreEqual(WavePhase.Active, sim.Waves.Phase);

            ClearCurrentWave(sim);

            Assert.AreEqual(WavePhase.Preparation, sim.Waves.Phase);
            Assert.AreEqual(2, sim.Waves.WaveNumber);
            Assert.Greater(sim.Waves.PreparationRemaining, 0f);
        }

        [Test]
        public void SeveralWavesCanRunBackToBack()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.BaseBudget = 10;
                config.Waves.EliteChance = 0f;
            });

            for (int i = 0; i < 5; i++)
            {
                sim.Waves.StartWaveNow();
                ClearCurrentWave(sim);
            }

            Assert.AreEqual(6, sim.Waves.WaveNumber);
            Assert.IsFalse(sim.IsGameOver);
        }

        [Test]
        public void ClearingAnEliteWaveDropsExactlyOneItem()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.BaseBudget = 10;
                config.Waves.EliteChance = 1f;
                config.Waves.EliteMinWave = 1;
            });

            Assert.AreEqual(0, sim.Inventory.Count);

            sim.Waves.StartWaveNow();
            Assert.IsTrue(sim.Waves.IsEliteWave);
            ClearCurrentWave(sim);

            Assert.AreEqual(1, sim.Inventory.Count, "Eine Elitewelle liefert genau ein Item.");
        }

        [Test]
        public void ClearingANormalWaveDropsNothing()
        {
            GameSimulation sim = TestSim.Create(1, config =>
            {
                config.Waves.BaseBudget = 10;
                config.Waves.EliteChance = 0f;
            });

            sim.Waves.StartWaveNow();
            ClearCurrentWave(sim);

            Assert.AreEqual(0, sim.Inventory.Count);
        }

        [Test]
        public void EliteEnemiesAreTougherThanNormalOnes()
        {
            GameSimulation sim = TestSim.Create();

            Enemy normal = sim.Enemies.Spawn(EnemyType.Ork, new Vec2(60f, 0f), false);
            Enemy elite = sim.Enemies.Spawn(EnemyType.Ork, new Vec2(60f, 5f), true);

            Assert.Greater(elite.MaxHealth, normal.MaxHealth);
            Assert.Greater(elite.DamageAgainstUnits, normal.DamageAgainstUnits);
        }

        [Test]
        public void LosingTheTownHallEndsTheGame()
        {
            GameSimulation sim = TestSim.Create();

            sim.Buildings.ApplyDamage(sim.Buildings.TownHall, 999999f);
            sim.Tick(0.1f);

            Assert.AreEqual(WavePhase.Defeat, sim.Waves.Phase);
            Assert.IsTrue(sim.IsGameOver);
        }

        [Test]
        public void HordesSpawnAwayFromTheSettlement()
        {
            GameSimulation sim = TestSim.Create(4, config => config.Waves.BaseBudget = 40);

            sim.Waves.StartWaveNow();
            TestSim.Run(sim, 3f);

            Assert.Greater(sim.Enemies.AliveCount, 0);
            for (int i = 0; i < sim.Enemies.Enemies.Count; i++)
            {
                float distance = sim.Enemies.Enemies[i].Position.Magnitude;
                Assert.Greater(distance, 30f, "Gegner duerfen nicht mitten in der Siedlung erscheinen.");
            }
        }

        /// <summary>
        /// Laesst die Gegner spawnen und raeumt sie ab, damit der Test den
        /// Wellenabschluss pruefen kann, ohne einen echten Kampf auszufechten.
        /// </summary>
        private static void ClearCurrentWave(GameSimulation sim)
        {
            for (int guard = 0; guard < 4000; guard++)
            {
                sim.Tick(0.1f);

                for (int i = sim.Enemies.Enemies.Count - 1; i >= 0; i--)
                {
                    sim.Enemies.ApplyDamage(sim.Enemies.Enemies[i], 999999f);
                }

                if (sim.Waves.Phase == WavePhase.Preparation)
                {
                    return;
                }
            }

            Assert.Fail("Die Welle wurde nicht abgeschlossen.");
        }

        [Test]
        public void EveryEnemyInTheEnumHasADefinition()
        {
            GameConfig config = DefaultContent.CreateConfig();

            foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
            {
                if (type == EnemyType.None)
                {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => config.GetEnemy(type),
                    "Fuer " + type + " fehlt eine EnemyDefinition.");
            }
        }
    }
}
