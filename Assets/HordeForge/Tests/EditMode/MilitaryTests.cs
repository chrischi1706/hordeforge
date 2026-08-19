using HordeForge.Core;
using HordeForge.Core.Data;
using HordeForge.Core.Military;
using NUnit.Framework;

namespace HordeForge.Tests
{
    [TestFixture]
    public class MilitaryTests
    {
        private GameSimulation _sim;

        [SetUp]
        public void SetUp()
        {
            _sim = TestSim.Create();

            // Ausruestung fuer die Rekrutierungstests bereitstellen.
            _sim.Resources.Set(ResourceType.Bow, 20);
            _sim.Resources.Set(ResourceType.Arrows, 500);
            _sim.Resources.Set(ResourceType.Spear, 20);
            _sim.Resources.Set(ResourceType.Shield, 20);
            _sim.Resources.Set(ResourceType.Sword, 20);
            _sim.Resources.Set(ResourceType.Armor, 20);
            _sim.Resources.Set(ResourceType.Bread, 400);
        }

        [Test]
        public void CreatingASquadConsumesCitizensAndEquipment()
        {
            int freeBefore = _sim.Population.Free;

            Squad squad;
            RecruitResult result = _sim.Squads.TryCreateSquad(
                new Vec2(20f, 0f), UnitType.Archer, 5, out squad);

            Assert.AreEqual(RecruitResult.Success, result);
            Assert.IsNotNull(squad);
            Assert.AreEqual(5, squad.Count);
            Assert.AreEqual(freeBefore - 5, _sim.Population.Free);
            Assert.AreEqual(15, _sim.Resources.Get(ResourceType.Bow), "5 Boegen verbraucht.");
            Assert.AreEqual(500 - 60, _sim.Resources.Get(ResourceType.Arrows), "12 Pfeile pro Bogenschuetze.");
        }

        [Test]
        public void SquadCreationFailsWithoutEnoughFreeCitizens()
        {
            // Alle bis auf drei Buerger binden.
            _sim.Population.TryReserve(_sim.Population.Free - 3);

            Squad squad;
            RecruitResult result = _sim.Squads.TryCreateSquad(
                new Vec2(20f, 0f), UnitType.Archer, 5, out squad);

            Assert.AreEqual(RecruitResult.NotEnoughCitizens, result);
            Assert.IsNull(squad, "Bei Fehlschlag darf keine leere Truppe zurueckbleiben.");
            Assert.AreEqual(20, _sim.Resources.Get(ResourceType.Bow), "Nichts darf verbraucht worden sein.");
        }

        [Test]
        public void SquadCreationFailsWithoutEnoughEquipment()
        {
            _sim.Resources.Set(ResourceType.Bow, 2);

            Squad squad;
            RecruitResult result = _sim.Squads.TryCreateSquad(
                new Vec2(20f, 0f), UnitType.Archer, 5, out squad);

            Assert.AreEqual(RecruitResult.NotEnoughEquipment, result);
            Assert.AreEqual(2, _sim.Resources.Get(ResourceType.Bow));
        }

        [Test]
        public void SquadCreationFailsWithoutEnoughFood()
        {
            _sim.Resources.Set(ResourceType.Bread, 0);
            _sim.Resources.Set(ResourceType.Fish, 0);

            Squad squad;
            RecruitResult result = _sim.Squads.TryCreateSquad(
                new Vec2(20f, 0f), UnitType.Archer, 5, out squad);

            Assert.AreEqual(RecruitResult.NotEnoughFood, result);
        }

        [Test]
        public void SquadsCanBeMixedAndExtended()
        {
            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(20f, 0f), UnitType.Archer, 5, out squad);

            Assert.AreEqual(RecruitResult.Success,
                _sim.Squads.TryAddUnits(squad, UnitType.Spearman, 2));
            Assert.AreEqual(RecruitResult.Success,
                _sim.Squads.TryAddUnits(squad, UnitType.Archer, 3));

            Assert.AreEqual(8, squad.CountOf(UnitType.Archer));
            Assert.AreEqual(2, squad.CountOf(UnitType.Spearman));
            Assert.AreEqual(10, squad.Count);
        }

        [Test]
        public void DisbandingReturnsCitizensAndEquipmentButNotAmmunition()
        {
            int freeBefore = _sim.Population.Free;

            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(20f, 0f), UnitType.Archer, 5, out squad);
            _sim.Squads.TryAddUnits(squad, UnitType.Spearman, 3);

            int arrowsAfterRecruiting = _sim.Resources.Get(ResourceType.Arrows);

            _sim.Squads.Disband(squad);

            Assert.AreEqual(freeBefore, _sim.Population.Free, "Alle 8 Buerger werden wieder frei.");
            Assert.AreEqual(20, _sim.Resources.Get(ResourceType.Bow), "5 Boegen kommen zurueck.");
            Assert.AreEqual(20, _sim.Resources.Get(ResourceType.Spear), "3 Speere kommen zurueck.");
            Assert.AreEqual(20, _sim.Resources.Get(ResourceType.Shield), "3 Schilde kommen zurueck.");
            Assert.AreEqual(arrowsAfterRecruiting, _sim.Resources.Get(ResourceType.Arrows),
                "Munition kommt nicht zurueck.");
            Assert.AreEqual(0, _sim.Squads.Squads.Count);
        }

        [Test]
        public void CanRecruitReportsTheBlockingReasonWithoutSpendingAnything()
        {
            _sim.Resources.Set(ResourceType.Sword, 0);

            Assert.AreEqual(
                RecruitResult.NotEnoughEquipment,
                _sim.Squads.CanRecruit(UnitType.Swordsman, 1));
            Assert.AreEqual(
                RecruitResult.Success,
                _sim.Squads.CanRecruit(UnitType.Archer, 1));
            Assert.AreEqual(20, _sim.Resources.Get(ResourceType.Bow), "Eine Pruefung darf nichts buchen.");
        }

        [Test]
        public void MovingASquadRepositionsItsUnits()
        {
            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(20f, 0f), UnitType.Spearman, 3, out squad);

            _sim.Squads.MoveSquad(squad, new Vec2(-15f, 10f));

            Assert.AreEqual(-15f, squad.Position.X, 0.01f);
            Assert.AreEqual(10f, squad.Position.Y, 0.01f);

            for (int i = 0; i < squad.Units.Count; i++)
            {
                float distance = Vec2.Distance(squad.Units[i].Position, squad.Position);
                Assert.Less(distance, 6f, "Die Einheiten muessen der Truppe folgen.");
            }
        }

        [Test]
        public void SquadPositionsStayInsideTheMap()
        {
            Squad squad;
            _sim.Squads.TryCreateSquad(new Vec2(20f, 0f), UnitType.Archer, 1, out squad);

            _sim.Squads.MoveSquad(squad, new Vec2(500f, 0f));

            Assert.LessOrEqual(squad.Position.Magnitude, _sim.Config.Settings.MapRadius + 0.01f);
        }

        [Test]
        public void EveryUnitInTheEnumHasADefinition()
        {
            foreach (UnitType type in System.Enum.GetValues(typeof(UnitType)))
            {
                if (type == UnitType.None)
                {
                    continue;
                }

                Assert.DoesNotThrow(
                    () => _sim.Config.GetUnit(type),
                    "Fuer " + type + " fehlt eine UnitDefinition.");
            }
        }
    }
}
