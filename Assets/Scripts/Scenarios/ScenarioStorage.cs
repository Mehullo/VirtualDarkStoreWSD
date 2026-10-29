using System;
using System.IO;
using UnityEngine;

namespace VirtualDarkStore.Scenarios
{
    /// <summary>
    /// Save/load of scenario definitions and recorded run results as human-readable JSON (Unity JsonUtility).
    /// </summary>
    public static class ScenarioStorage
    {
        public static string ToJson(ScenarioDefinition scenario)
        {
            if (scenario == null) throw new ArgumentNullException(nameof(scenario));
            return JsonUtility.ToJson(scenario, true);
        }

        public static ScenarioDefinition FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) throw new ArgumentException("Scenario JSON is empty.", nameof(json));
            var scenario = JsonUtility.FromJson<ScenarioDefinition>(json);
            if (scenario == null) throw new InvalidDataException("Scenario JSON could not be parsed.");
            if (!scenario.Validate(out string error)) throw new InvalidDataException($"Invalid scenario: {error}");
            return scenario;
        }

        public static void Save(ScenarioDefinition scenario, string path)
        {
            if (!scenario.Validate(out string error)) throw new InvalidDataException($"Invalid scenario: {error}");
            WriteText(path, ToJson(scenario));
        }

        public static ScenarioDefinition Load(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Scenario file not found.", path);
            return FromJson(File.ReadAllText(path));
        }

        public static void SaveRun(ScenarioRunResult run, string path)
        {
            if (run == null) throw new ArgumentNullException(nameof(run));
            WriteText(path, JsonUtility.ToJson(run, true));
        }

        public static ScenarioRunResult LoadRun(string path)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Run file not found.", path);
            var run = JsonUtility.FromJson<ScenarioRunResult>(File.ReadAllText(path));
            if (run == null || run.scenario == null || run.events == null) throw new InvalidDataException("Run JSON could not be parsed.");
            return run;
        }

        public static void WriteText(string path, string content)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, content);
        }
    }
}
