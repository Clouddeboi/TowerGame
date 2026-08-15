using System.Collections.Generic;
using Game.Inventory.SaveSystem;
using NUnit.Framework;

namespace Game.Inventory.Tests
{
    public class SaveMigrationRunnerTests
    {
        private class TestMigrationV1ToV2 : ISaveMigrationStep
        {
            public int FromSchemaVersion => 1;
            public List<string> appliedTo;

            public void Apply(InventorySystemSaveRecord record)
            {
                appliedTo?.Add("v1_to_v2");
            }
        }

        private class TestMigrationV2ToV3 : ISaveMigrationStep
        {
            public int FromSchemaVersion => 2;
            public List<string> appliedTo;

            public void Apply(InventorySystemSaveRecord record)
            {
                appliedTo?.Add("v2_to_v3");
            }
        }

        [Test]
        public void Migrate_AlreadyAtCurrentVersion_AppliesNothing()
        {
            var record = new InventorySystemSaveRecord { schemaVersion = 3 };
            var runner = new SaveMigrationRunner(new ISaveMigrationStep[] { new TestMigrationV1ToV2(), new TestMigrationV2ToV3() }, 3);
            var report = new SaveLoadReport();

            int applied = runner.Migrate(record, report);

            Assert.That(applied, Is.EqualTo(0));
            Assert.That(record.schemaVersion, Is.EqualTo(3));
        }

        [Test]
        public void Migrate_SingleHop_AppliesOneStepAndBumpsVersion()
        {
            var appliedLog = new List<string>();
            var record = new InventorySystemSaveRecord { schemaVersion = 1 };
            var step = new TestMigrationV1ToV2 { appliedTo = appliedLog };
            var runner = new SaveMigrationRunner(new ISaveMigrationStep[] { step }, 2);
            var report = new SaveLoadReport();

            int applied = runner.Migrate(record, report);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(record.schemaVersion, Is.EqualTo(2));
            Assert.That(appliedLog, Is.EqualTo(new[] { "v1_to_v2" }));
        }

        [Test]
        public void Migrate_MultiHop_AppliesEveryStepInOrder()
        {
            var appliedLog = new List<string>();
            var record = new InventorySystemSaveRecord { schemaVersion = 1 };
            var stepA = new TestMigrationV1ToV2 { appliedTo = appliedLog };
            var stepB = new TestMigrationV2ToV3 { appliedTo = appliedLog };
            var runner = new SaveMigrationRunner(new ISaveMigrationStep[] { stepB, stepA }, 3);
            var report = new SaveLoadReport();

            int applied = runner.Migrate(record, report);

            Assert.That(applied, Is.EqualTo(2));
            Assert.That(record.schemaVersion, Is.EqualTo(3));
            Assert.That(appliedLog, Is.EqualTo(new[] { "v1_to_v2", "v2_to_v3" }));
        }

        [Test]
        public void Migrate_NewerThanSupported_LeavesUntouchedAndWarns()
        {
            var record = new InventorySystemSaveRecord { schemaVersion = 5 };
            var runner = new SaveMigrationRunner(new ISaveMigrationStep[0], 3);
            var report = new SaveLoadReport();

            int applied = runner.Migrate(record, report);

            Assert.That(applied, Is.EqualTo(0));
            Assert.That(record.schemaVersion, Is.EqualTo(5));
            Assert.That(report.warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Migrate_MissingIntermediateStep_StopsAndWarns()
        {
            var record = new InventorySystemSaveRecord { schemaVersion = 1 };
            // only a v1->v2 step exists, but current version is 3 - no v2->v3 step registered
            var runner = new SaveMigrationRunner(new ISaveMigrationStep[] { new TestMigrationV1ToV2() }, 3);
            var report = new SaveLoadReport();

            int applied = runner.Migrate(record, report);

            Assert.That(applied, Is.EqualTo(1));
            Assert.That(record.schemaVersion, Is.EqualTo(2));
            Assert.That(report.warnings.Count, Is.GreaterThan(0));
        }
    }
}