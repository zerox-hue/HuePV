using HueHelper;
using Life;
using Life.BizSystem;
using ModKit.Interfaces;
using System;
using System.Collections.Generic;
using _menu = AAMenu.Menu;
using Life.Network;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Life.UI;
using UnityEngine;
using Mirror;
using Life.DB;
using static ModKit.Utils.IconUtils;
using System.IO;
using ModKit.Helper;


namespace HuePV
{
    public class HuePV : ModKit.ModKit
    {
        public HuePV(IGameAPI aPI) : base(aPI) { }
        public static Config config;
        public class Config
        {
            public int Prix;
            public int LevelMinimumToDeletePV;
        }
        public void CreateConfig()
        {
            string directoryPath = pluginsPath + "/HuePV";

            string configFilePath = directoryPath + "/config.json";

            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            if (!File.Exists(configFilePath))
            {
                var defaultConfig = new Config
                {
                    Prix = 100,
                    LevelMinimumToDeletePV = 3,
                };
                string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(defaultConfig, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(configFilePath, jsonContent);
            }

            config = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(File.ReadAllText(configFilePath));
        }
        public override void OnPluginInit()
        {
            base.OnPluginInit();
            AllHelper.InitHelper.InitMessage("V.1.0.0", "Zerox_Hue");
            Orm.RegisterTable<OrmClassPV>();
            AddTabLineLawEnforcement();
            AddTabLineLawEnforcementViewAllPV();
            AddTabLineAdministrationViewAll();
            CreateConfig();
            Nova.server.OnMinutePassedEvent += new Action(PayPV);
        }
        public async void PayPV()
        {
            foreach (var players in Nova.server.Players)
            {
                foreach (var vehicles in Nova.v.vehicles)
                {
                    if (vehicles.permissions.owner.characterId == players.character.Id)
                    {
                        List<OrmClassPV> elements = await OrmClassPV.Query(x => x.Plaque == vehicles.plate);
                        if (elements.Any())
                        {
                            foreach (var Allelements in elements)
                            {

                                players.AddBankMoney(-config.Prix);

                                players.SendText($"<color=#e82727>[HuePV]</color> <color=#59c22d><b>{config.Prix.ToString()}</b></color> € ont été enlevé de ton compte en banque car un policier a mis un Pv sur ton véhicule ayant cette plaque : <color=#5c2dc2><b>{Allelements.Plaque}</b></color> !");

                                await Task.Delay(1);

                                Allelements.Plaque = "Delete";

                                Allelements.Payer = true;

                                await Allelements.Delete();

                                await Allelements.Save();
                            }
                        }

                    }

                }

            }
        }
        public void AddTabLineLawEnforcement()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, $"<color=#2dc24d>Mettre un PV</color>", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);

                OnClickPV(player);
            });
        }
        public void AddTabLineLawEnforcementViewAllPV()
        {
            _menu.AddBizTabLine(PluginInformations, new List<Activity.Type> { Activity.Type.LawEnforcement }, null, $"<color=#2dc24d>Voir tous les PV</color>", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);

                ViewAllPvLawEnforcement(player);
            });
        }
        public void AddTabLineAdministrationViewAll()
        {
            _menu.AddAdminTabLine(PluginInformations, 1, "<color=#2dc24d>Voir tous les PV</color>", (ui) =>
            {
                Player player = PanelHelper.ReturnPlayerFromPanel(ui);
                ViewAllPvAdmin(player);
            });
        }
        public async void ViewAllPvAdmin(Player player)
        {
            var Allelements = await OrmClassPV.Query(x => x.Payer == false);

            Panel panel = PanelHelper.Create($"ALL PV", UIPanel.PanelType.TabPrice, player, () => ViewAllPvAdmin(player));

            AllHelper.PanelHelper.CloseButton(player, panel);

            if (Allelements.Any())
            {
                foreach (var elements in Allelements)
                {
                    panel.AddTabLine($"{elements.Plaque}", "", AllHelper.IconHelper.GetVehicleIcon(44), ui =>
                    {
                        Panel panel1 = PanelHelper.Create($"PV Admin : <color=#2dc259><i>{elements.Plaque}</i></color>", UIPanel.PanelType.Text, player, () => ViewAllPvAdmin(player));

                        AllHelper.PanelHelper.CloseButton(player, panel1);

                        panel1.AddButton("Supprimer", async ui1 =>
                        {
                            if (player.account.adminLevel >= config.LevelMinimumToDeletePV)
                            {
                                player.ClosePanel(ui1);
                                elements.Plaque = "Delete";
                                elements.Payer = true;
                                await elements.Save();
                                player.Notify("Succés", "Tu as supprimé la contravention avec succés !", NotificationManager.Type.Success);
                            }
                            else
                            {
                                player.SendText($"<color=#e82727>[HuePV]</color> Tu n'es pas administrateur {config.LevelMinimumToDeletePV.ToString()} ou plus !");
                            }
                        });

                        panel1.TextLines.Add($"Plaque : <i><color=#2dc259>{elements.Plaque}</color></i>");

                        panel1.Display();
                    });
                }
            }
            else
            {
                panel.AddTabLine("<color=#e82727>Aucun PV Actif</color>", ui => player.ClosePanel(ui));
            }

            AllHelper.PanelHelper.ValidButton(player, panel);

            panel.Display();
        }
        public async void ViewAllPvLawEnforcement(Player player)
        {
            var Allelements = await OrmClassPV.Query(x => x.Payer == false);

            Panel panel = PanelHelper.Create("Tous Les PV", UIPanel.PanelType.TabPrice, player, () => ViewAllPvLawEnforcement(player));

            AllHelper.PanelHelper.CloseButton(player, panel);

            if (Allelements.Any())
            {
                foreach (var elements in Allelements)
                {
                    panel.AddTabLine($"{elements.Plaque}", "", AllHelper.IconHelper.GetVehicleIcon(44), ui =>
                    {
                        Panel panel1 = PanelHelper.Create($"PV : <color=#2dc259><i>{elements.Plaque}</i></color>", UIPanel.PanelType.Text, player, () => ViewAllPvLawEnforcement(player));

                        AllHelper.PanelHelper.CloseButton(player, panel1);

                        panel1.AddButton("Supprimer", async ui1 =>
                        {
                            player.ClosePanel(ui1);
                            elements.Plaque = "Delete";
                            elements.Payer = true;
                            await elements.Save();
                            player.Notify("Succés" , "Tu as supprimé la contravention avec succés !", NotificationManager.Type.Success);
                        });

                        panel1.TextLines.Add($"Plaque : <i><color=#2dc259>{elements.Plaque}</color></i>");

                        panel1.Display();
                    });
                }
            }
            else
            {
                panel.AddTabLine("<color=#e82727>Aucun PV Actif</color>", ui => player.ClosePanel(ui));
            }

            AllHelper.PanelHelper.ValidButton(player, panel);

            panel.Display();
        }
        public void OnClickPV(Player player)
        {
            Panel panel = PanelHelper.Create("HuePV", UIPanel.PanelType.Input, player, () => OnClickPV(player));

            panel.SetInputPlaceholder("Saisissez la plaque du véhicule...");

            AllHelper.PanelHelper.CloseButton(player, panel);

            panel.AddButton("Valider", ui =>
            {
                player.ClosePanel(ui);

                OnClickValid(player, panel);
            });

            panel.Display();
        }
        public async void OnClickValid(Player player, UIPanel panel)
        {
            var elements = await OrmClassPV.Query(x => x.Plaque == panel.inputText);
            if (!elements.Any())
            {
                    OrmClassPV instance = new OrmClassPV();
                    instance.Plaque = panel.inputText;
                    instance.Payer = false;
                    await instance.Save();
                    bool result = await instance.Save();

                    if (result)
                    {
                        Debug.Log("Sauvegarde Réussi");
                        player.SendText($"<color=#e82727>[HuePV]</color> Le Pv a bien été appliqué !");
                    }
                    else
                    {
                        Debug.Log("Sauvegarde Impossible");
                        player.SendText($"<color=#e82727>[HuePV]</color> Il y'a eu une erreur lors du chargement du Pv merci de réessayer ultérirement si le probléme persiste merci d'en parler à un staff !");
                    }
            }
            else
            {
                player.SendText($"<color=#e82727>[HuePV]</color> Cette plaque a déjà une contravention !");
            }

        }
    }
}
