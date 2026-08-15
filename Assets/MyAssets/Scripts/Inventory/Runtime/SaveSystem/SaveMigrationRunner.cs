using System.Collections.Generic;

namespace Game.Inventory.SaveSystem
{
    //applies every registered migration step in order, walking a save record forward
    //from whatever version it was saved at up to currentSchemaVersion
    //registered steps do not need to be pre-sorted, the runner sorts by FromSchemaVersion
    public class SaveMigrationRunner
    {
        private readonly List<ISaveMigrationStep> _steps;
        private readonly int _currentSchemaVersion;

        public SaveMigrationRunner(IEnumerable<ISaveMigrationStep> steps, int currentSchemaVersion)
        {
            _steps = new List<ISaveMigrationStep>(steps);
            _steps.Sort((a, b) => a.FromSchemaVersion.CompareTo(b.FromSchemaVersion));
            _currentSchemaVersion = currentSchemaVersion;
        }

        //migrates the record in place, returning the number of steps applied, a save
        //already at the current version applies zero steps and is left untouched
        //a save from a future, newer version than this runner knows about is left as-is
        //and flagged in the report rather than guessed at or corrupted
        public int Migrate(InventorySystemSaveRecord record, SaveLoadReport report)
        {
            if (record.schemaVersion > _currentSchemaVersion)
            {
                report.warnings.Add($"Save schema version {record.schemaVersion} is newer than this build supports ({_currentSchemaVersion}). Loading without migration; some data may not be recognized.");
                return 0;
            }

            int appliedCount = 0;

            foreach (ISaveMigrationStep step in _steps)
            {
                if (step.FromSchemaVersion != record.schemaVersion)
                {
                    continue;
                }

                step.Apply(record);
                record.schemaVersion = step.FromSchemaVersion + 1;
                appliedCount++;
            }

            //a save might need more than one hop (e.g. version 1 to 4 via three steps),
            //so keep walking forward until no further applicable step exists or we
            //reach the current version
            while (record.schemaVersion < _currentSchemaVersion)
            {
                bool foundNextStep = false;

                foreach (ISaveMigrationStep step in _steps)
                {
                    if (step.FromSchemaVersion != record.schemaVersion)
                    {
                        continue;
                    }

                    step.Apply(record);
                    record.schemaVersion = step.FromSchemaVersion + 1;
                    appliedCount++;
                    foundNextStep = true;
                    break;
                }

                if (!foundNextStep)
                {
                    report.warnings.Add($"No migration step found to bring save data from version {record.schemaVersion} to {_currentSchemaVersion}. Save data may be incomplete.");
                    break;
                }
            }

            return appliedCount;
        }
    }
}