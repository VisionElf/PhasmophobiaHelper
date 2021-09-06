using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NonInvasiveKeyboardHookLibrary;

namespace PhasmophobiaHelper
{
    public static class Extensions
    {
        public static bool Contains<T>(this T[] array, T value) where T : class
        {
            if (array == null)
                return false;
            foreach (var elt in array)
            {
                if (elt == value)
                    return true;
            }
            return false;
        }
    }
    
    public class Phasmophobia
    {
        private static List<Ghost> _ghosts;
    
        private static int _evidencesCount = Enum.GetValues(typeof(GhostEvidence)).Length;
        
        private List<GhostEvidence> _currentEvidences = new List<GhostEvidence>();
        private List<GhostEvidence> _currentNotEvidences = new List<GhostEvidence>();

        private KeyboardHookManager _keyboard;

        public event Action OnStateChanged;

        private struct ShortcutInfo
        {
            public int hexCode;
            public string key;
            public GhostEvidence evidence;
        }

        private ShortcutInfo[] _shortcuts;

        public Phasmophobia()
        {
            InitializeGhostList();
            InitializeShortcuts();
        }

        private void InitializeShortcuts()
        {
            _keyboard = new KeyboardHookManager();
            _keyboard.Start();
            
            _shortcuts = new[]
            {
                new ShortcutInfo
                {
                    hexCode = 0x70, key = "F1", evidence = GhostEvidence.Orb
                },
                new ShortcutInfo
                {
                    hexCode = 0x71, key = "F2", evidence = GhostEvidence.SpiritBox
                },
                new ShortcutInfo
                {
                    hexCode = 0x72, key = "F3", evidence = GhostEvidence.DOTS
                },
                new ShortcutInfo
                {
                    hexCode = 0x73, key = "F4", evidence = GhostEvidence.Fingerprints
                },
                new ShortcutInfo
                {
                    hexCode = 0x74, key = "F5", evidence = GhostEvidence.Freezing
                },
                new ShortcutInfo
                {
                    hexCode = 0x75, key = "F6", evidence = GhostEvidence.Writing
                },
                new ShortcutInfo
                {
                    hexCode = 0x76, key = "F7", evidence = GhostEvidence.EMF5
                },
            };

            _keyboard.RegisterHotkey(0x77, ClearAllEvidences);

            foreach (var shortcut in _shortcuts)
            {
                _keyboard.RegisterHotkey(ModifierKeys.Control, shortcut.hexCode, () =>
                {
                    ToggleNotEvidence(shortcut.evidence);
                });
                
                _keyboard.RegisterHotkey(shortcut.hexCode, () =>
                {
                    ToggleEvidence(shortcut.evidence);
                });
            }
        }

        public string GetText1()
        {
            if (_currentEvidences.Count >= 1)
                return _currentEvidences[0].ToString();
            
            if (_currentNotEvidences.Count > 0)
            {
                var possibleGhosts =
                    GetGhostsThatHaveEvidences(_currentEvidences.ToArray(), _currentNotEvidences.ToArray());
                var commonEvidences = GetCommonEvidences(possibleGhosts);
                if (commonEvidences.Count > 0)
                {
                    var commonText = $"It's: \n\n";
                    commonText += string.Join("\n", commonEvidences);

                    return commonText;
                }
                return "";
            }
            return "Press a key to start.";
        }
        
        public string GetText2()
        {
            if (_currentEvidences.Count >= 2)
                return _currentEvidences[1].ToString();
            
            if (_currentEvidences.Count == 1)
            {
                var possibleGhosts =
                    GetGhostsThatHaveEvidences(_currentEvidences.ToArray(), _currentNotEvidences.ToArray());
                var commonEvidences = GetCommonEvidences(possibleGhosts);
                var commonText = "";
                foreach (var ev in _currentEvidences)
                {
                    if (commonEvidences.Contains(ev))
                        commonEvidences.Remove(ev);
                }
                
                if (commonEvidences.Count > 0)
                    commonText = $"It's {commonEvidences[0]}\n\n";
                
                var possibleEvidences = GetPossibleEvidences(_currentEvidences.ToArray(), _currentNotEvidences.ToArray());
                var ignoreEvidences = GetOtherEvidences(possibleEvidences.ToArray());

                foreach (var ev in _currentEvidences)
                    possibleEvidences.Remove(ev);
                foreach (var ev in commonEvidences)
                    possibleEvidences.Remove(ev);
                
                foreach (var nEv in _currentNotEvidences)
                    ignoreEvidences.Remove(nEv);

                var ignoreText = "Ignore\n\n" + string.Join("\n", ignoreEvidences);
                var possibleText = commonText + "Search\n\n" + string.Join("\n", possibleEvidences);

                if (ignoreEvidences.Count > 0 && possibleEvidences.Count >= 4)
                    return ignoreText;
                return possibleText;
            }
            return "";
        }

        public string GetText3()
        {
            if (_currentEvidences.Count >= 3)
                return _currentEvidences[2].ToString();
            
            if (_currentEvidences.Count == 2)
            {
                var possibleEvidences = GetPossibleEvidences(_currentEvidences.ToArray(), _currentNotEvidences.ToArray());
                
                foreach (var ev in _currentEvidences)
                    possibleEvidences.Remove(ev);

                var possibleText = "Search\n\n" + string.Join("\n", possibleEvidences);

                return possibleText;
            }
            return "";
        }
        
        public string GetText4()
        {
            var ghosts = GetGhostsThatHaveEvidences(_currentEvidences.ToArray(), _currentNotEvidences.ToArray());
            if (ghosts.Count <= 0)
                return "Wrong evidences";
            
            if (ghosts.Count <= 9)
                return string.Join("\n", ghosts.Select(ghost => ghost.type.ToString()));
            return "Not enough\nEvidences";
        }

        private List<GhostEvidence> GetCommonEvidences(List<Ghost> ghosts)
        {
            var result = new List<GhostEvidence>();
            foreach (var ghost in ghosts)
            {
                foreach (var ev in ghost.Evidences)
                {
                    if (!result.Contains(ev))
                        result.Add(ev);
                }
            }

            foreach (var ghost in ghosts)
            {
                foreach (var ev in result.ToArray())
                {
                    if (!ghost.HasEvidence(ev))
                        result.Remove(ev);
                }
            }
            
            return result;
        }

        public string GetShortcuts()
        {
            return string.Join(", ", _shortcuts.Select(info => $"{info.key}:{info.evidence}" ));
        }
        
        public void ClearAllEvidences()
        {
            _currentEvidences.Clear();
            _currentNotEvidences.Clear();
            OnStateChanged?.Invoke();
        }
        
        private List<GhostEvidence> GetPossibleEvidences(GhostEvidence[] evidences, GhostEvidence[] notEvidences = null)
        {
            var result = new List<GhostEvidence>();
            var ghosts = GetGhostsThatHaveEvidences(evidences, notEvidences);
            
            foreach (var ghost in ghosts)
            {
                foreach (var ev in ghost.Evidences)
                {
                    if (!result.Contains(ev))
                    {
                        result.Add(ev);
                    }
                }
            }

            result.Sort();
            return result;
        }

        private void ToggleEvidence(GhostEvidence ev)
        {
            if (_currentEvidences.Contains(ev))
                _currentEvidences.Remove(ev);
            else if (_currentEvidences.Count < 3)
                _currentEvidences.Add(ev);
            OnStateChanged?.Invoke();
        }
        
        private void ToggleNotEvidence(GhostEvidence ev)
        {
            if (_currentNotEvidences.Contains(ev))
                _currentNotEvidences.Remove(ev);
            else
                _currentNotEvidences.Add(ev);
            OnStateChanged?.Invoke();
        }

        public void Destroy()
        {
            _keyboard.UnregisterAll();
            _keyboard.Stop();
        }
    
        private static void InitializeGhostList()
        {
            if (_ghosts != null) return;
        
            _ghosts = new List<Ghost>
            {
                new Ghost(
                    GhostType.Banshee,
                    GhostEvidence.DOTS,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.Orb
                ),
                new Ghost(
                    GhostType.Demon,
                    GhostEvidence.Writing,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Jinn,
                    GhostEvidence.EMF5,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Mare,
                    GhostEvidence.Writing,
                    GhostEvidence.Orb,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Oni,
                    GhostEvidence.DOTS,
                    GhostEvidence.EMF5,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Phantom,
                    GhostEvidence.DOTS,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Poltergeist,
                    GhostEvidence.Writing,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Revenant,
                    GhostEvidence.Writing,
                    GhostEvidence.Orb,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Shade,
                    GhostEvidence.Writing,
                    GhostEvidence.EMF5,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Spirit,
                    GhostEvidence.Writing,
                    GhostEvidence.EMF5,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Wraith,
                    GhostEvidence.DOTS,
                    GhostEvidence.EMF5,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Yurei,
                    GhostEvidence.DOTS,
                    GhostEvidence.Orb,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Yokai,
                    GhostEvidence.DOTS,
                    GhostEvidence.Orb,
                    GhostEvidence.SpiritBox
                ),
                new Ghost(
                    GhostType.Hantu,
                    GhostEvidence.Orb,
                    GhostEvidence.Fingerprints,
                    GhostEvidence.Freezing
                ),
                new Ghost(
                    GhostType.Myling,
                    GhostEvidence.Writing,
                    GhostEvidence.EMF5,
                    GhostEvidence.Fingerprints
                ),
                new Ghost(
                    GhostType.Goryo,
                    GhostEvidence.DOTS,
                    GhostEvidence.EMF5,
                    GhostEvidence.Fingerprints
                )
            };
        }

        public static List<Ghost> GetGhostsThatHaveEvidences(GhostEvidence[] evidences, GhostEvidence[] notEvidences)
        {
            var result = new List<Ghost>();
            foreach (var ghost in _ghosts)
            {
                if (ghost.HasAnyEvidences(notEvidences)) continue;
                
                if (ghost.HasAllEvidences(evidences))
                    result.Add(ghost);
            }

            return result;
        }
    
        public static List<Ghost> GetGhostsThatDontHaveEvidences(params GhostEvidence[] evidences)
        {
            var result = new List<Ghost>();
            foreach (var ghost in _ghosts)
            {
                if (ghost.HasAnyEvidences(evidences))
                {
                    result.Add(ghost);
                }
            }
            return result;
        }

        public static List<GhostEvidence> GetOtherEvidences(params GhostEvidence[] evidences)
        {
            var others = new List<GhostEvidence>();
            for (var i = 0; i < _evidencesCount; i++)
            {
                var ev = (GhostEvidence) i;
                if (!evidences.Contains(ev))
                    others.Add(ev);
            }
            others.Sort();
            return others;
        }

        public string GetNotEvidences()
        {
            return "Not: " + string.Join(", ", _currentNotEvidences.Select(ev => ev.ToString()));
        }
    }

    [Serializable]
    public struct Ghost
    {
        public GhostType type;
        public GhostEvidence evidence1;
        public GhostEvidence evidence2;
        public GhostEvidence evidence3;

        public Ghost(GhostType type, GhostEvidence evidence1, GhostEvidence evidence2, GhostEvidence evidence3)
        {
            this.type = type;
            this.evidence1 = evidence1;
            this.evidence2 = evidence2;
            this.evidence3 = evidence3;
        }

        public GhostEvidence[] Evidences
        {
            get
            {
                return new [] {evidence1, evidence2, evidence3};   
            }
        }

        public bool HasEvidence(GhostEvidence evidence)
        {
            return evidence1 == evidence || evidence2 == evidence || evidence3 == evidence;
        }

        public GhostEvidence GetLastEvidence(GhostEvidence ev1, GhostEvidence ev2)
        {
            if (evidence1 != ev1 && evidence1 != ev2) return evidence1;
            if (evidence2 != ev1 && evidence2 != ev2) return evidence2;
            if (evidence3 != ev1 && evidence3 != ev2) return evidence3;
            return evidence1;
        }

        public GhostEvidence[] GetLastEvidences(GhostEvidence ev1)
        {
            if (ev1 == evidence1) return new[] {evidence2, evidence3};
            if (ev1 == evidence2) return new[] {evidence1, evidence3};
            if (ev1 == evidence3) return new[] {evidence1, evidence2};
            return new GhostEvidence[0];
        }

        public bool HasAllEvidences(GhostEvidence[] evidences)
        {
            foreach (var ev in evidences)
            {
                if (!HasEvidence(ev))
                    return false;
            }

            return true;
        }

        public bool HasAnyEvidences(GhostEvidence[] evidences)
        {
            foreach (var ev in evidences)
            {
                if (HasEvidence(ev))
                    return true;
            }
        
            return false;
        }
    }

    public enum GhostType
    {
        Spirit,
        Wraith,
        Phantom,
        Poltergeist,
        Banshee,
        Jinn,
        Mare,
        Revenant,
        Shade,
        Demon,
        Yurei,
        Oni,
        Yokai,
        Hantu,
        Myling,
        Goryo,
    }

    public enum GhostEvidence
    {
        EMF5,
        SpiritBox,
        Fingerprints,
        Orb,
        Writing,
        Freezing,
        DOTS,
    }
}