using System;
using System.IO;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;
namespace Flightbeat.Editor {
    public static class GardenDefaultEvidence {
        [Serializable] sealed class DefaultRecord {
            public int activeFighter, fighterLevel, noseLevel, weaponKind, weaponLevel, weaponForm, formalFinaleMode, teachingFinaleMode;
            public string fighterName, weaponName, weaponNote, gardenTalent, signatureName, note;
        }
        public static void Write(string output) {
            // Isolated default profile: no PlayerPrefs reads or writes.
            var economy = new FleetEconomy(new FleetProfile(), () => 1791849600L, _ => { }, _ => 0);
            int model = economy.ActiveFighter, level = economy.FighterLevel(model), nose = economy.PartLevel(model, 0);
            var weapon = WeaponProfile.For(model, nose);
            var result = new DefaultRecord { activeFighter = model, fighterName = FleetCatalog.FighterNames[model], fighterLevel = level,
                noseLevel = nose, weaponKind = (int)weapon.kind, weaponLevel = weapon.level, weaponForm = weapon.Form,
                weaponName = WeaponProfile.Names[(int)weapon.kind], weaponNote = weapon.Note, gardenTalent = "分裂果",
                formalFinaleMode = Finale.ModeFor(model, level), teachingFinaleMode = 0, signatureName = FleetCatalog.FighterFinale[model],
                note = "New profile uses Pulse form 1 without splitting; signature Pulse Nova is only selected at fighter stage >= 3. Lessons explicitly use finale mode 0." };
            if (model != 0 || level != 1 || nose != 1 || weapon.Form != 1 || result.formalFinaleMode != 0)
                throw new Exception("Default profile no longer matches R1 step 0 evidence.");
            File.WriteAllText(output, JsonUtility.ToJson(result, true));
        }
    }
}
