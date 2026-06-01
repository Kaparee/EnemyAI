using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

public class ShipStats : MonoBehaviour {
    public float CurrentHP { get; private set; }
    public float CurrentEnergy { get; private set; }
    public float CurrentCargo { get; private set; }
    public bool IsDestroyed { get; private set; }
    public float MaxMainThrust { get => maxMainThrust; set => maxMainThrust = value; }
    public float BrakeThrust { get => brakeThrust; set => brakeThrust = value; }
    public float ManeuverForce { get => maneuverForce; set => maneuverForce = value; }
    public float RollForce { get => rollForce; set => rollForce = value; }
    public float LiftThrust { get => liftThrust; set => liftThrust = value; }
    public float EmergencySpeedMultiplier { get => emergencySpeedMultiplier; set => emergencySpeedMultiplier = value; }
    public float NormalDrainRate { get => normalDrainRate; set => normalDrainRate = value; }
    public float LowFuelThreshold { get => lowFuelThreshold; set => lowFuelThreshold = value; }

    [SerializeField] private float MaxHP;
    [SerializeField] private float MaxEnergy;
    [SerializeField] private float MaxCargo;
    [SerializeField] private float BaseMass;
    [SerializeField] private List<string> purchasedUpgrades = new List<string>();

    [SerializeField] private float maxMainThrust = 800000f;
    [SerializeField] private float brakeThrust = 400000f;
    [SerializeField] private float maneuverForce = 120000f;
    [SerializeField] private float rollForce = 120000f;
    [SerializeField] private float liftThrust = 120000f;
    [SerializeField] private float emergencySpeedMultiplier = 0.3f;
    [SerializeField] private float normalDrainRate = 1f;
    [SerializeField] private float lowFuelThreshold = 40f;

    public float baseCargoCapacity = 100f;
    public float currentMaxCargo;

    [Header("--- PANCERZ STREFOWY ---")]
    public float frontArmorMultiplier = 0.5f;
    public float sideArmorMultiplier = 1.0f;
    public float rearArmorMultiplier = 1.5f;

    [Header("--- SKRYPTY STERUJĄCE DO ZABLOKOWANIA ---")]
    [SerializeField] private MonoBehaviour[] controlScriptsToDisable;

    public void Start() {

        if (CurrentHP <= 0) CurrentHP = MaxHP;
        if (CurrentEnergy <= 0) CurrentEnergy = MaxEnergy;
        if (CurrentCargo <= 0) CurrentCargo = 0;
    }

    public void ResetData()
    {
        CurrentHP = MaxHP;
        CurrentEnergy = MaxEnergy;
        CurrentCargo = 0;
    }

    //*****************************88888  
    public void UpdateMaxCargo(float multiplier) 
    {
        MaxCargo = baseCargoCapacity * multiplier;
        Debug.Log($"[ShipStats] Nowy limit ładowni: {MaxCargo}");
    }

    

    //****************88888
    public void TakeZonedDamage(float baseDamage, Vector3 hitNormal)
    {
        float damage = baseDamage;
        float dotForward = Vector3.Dot(transform.forward, hitNormal);
        float dotRight = Vector3.Dot(transform.right, hitNormal);

        if (Mathf.Abs(dotForward) > Mathf.Abs(dotRight))
        {
            if (dotForward > 0) damage *= frontArmorMultiplier;
            else damage *= rearArmorMultiplier;
        }
        else
        {
            damage *= sideArmorMultiplier;
        }

        TakeDamage(damage);
    }

    public void TakeDamage(float damage)
    {
        if (damage > 0f)
        {
            CurrentHP = CurrentHP - damage;
            Debug.Log("Ustawiono wartość HP na: " + CurrentHP);
            if (CurrentHP <= 0f)
            {
                CurrentHP = 0f;
                if (!IsDestroyed)
                {
                    IsDestroyed = true;
                    HandleDestruction();
                }
                Debug.Log("Statek zniszczony!");
            }
        }
        else
        {
            Debug.Log("Nie możesz zadać statkowi mniej niż 0 dmg");
        }
    }

    private void HandleDestruction()
    {
        Debug.Log("<color=red>STATEK ZNISZCZONY!</color>");

        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Die();
        }
        else if (GameManager.Instance != null)
        {
            GameManager.Instance.ChangeState(GameState.GameOver);
        }
    }

    public void Heal(float amount) {
        if (amount > 0f) {
            if (CurrentHP + amount > MaxHP) {
                CurrentHP = MaxHP;
                Debug.Log("Statek naprawiony!");
                IsDestroyed = false;
            }
            else {
                CurrentHP += amount;
                IsDestroyed = false;
            }
            Debug.Log("Ustawiono wartość HP na: " + CurrentHP);
        }
        else {
            Debug.Log("Nie możesz uleczyć statku za mniej niż 0HP");
        }
    }

    public void UseEnergy(float amount) {
        if (amount > 0f) {
            if (CurrentEnergy < amount) {
                CurrentEnergy = 0;
                Debug.Log("Statek nie ma paliwa!");
            }
            else {
                CurrentEnergy -= amount;
            }
        }
        else {
            Debug.Log("Nie możesz spalić mniej niż 0 jednostek paliwa");
        }
    }

    public void AddEnergy(float amount) {
        if (amount > 0f) {
            if (CurrentEnergy + amount > MaxEnergy) {
                CurrentEnergy = MaxEnergy;
                Debug.Log("Statek zatankowany na full!");
            }
            else {
                CurrentEnergy += amount;
            }
            Debug.Log("Ustawiono wartość Paliwa na: " + CurrentEnergy);
        }
        else {
            Debug.Log("Nie możesz zatankować statku za mniej niż 0 jednostek paliwowych");
        }
    }

    public bool AddCargo(float amount) {
        if (amount > 0) {
            if (CurrentCargo + amount > MaxCargo) {
                return false;
            } else {
                CurrentCargo += amount;
                return true;
            }
        } else {
            return false;
        }
    }

    public float GetTotalMass() {
        return BaseMass + CurrentCargo;
    }

    public void SetHP(float amount) {
        CurrentHP = amount;
        Debug.Log("Poprawnie przypisano " + amount + " HP");
    }
    public void SetCargo(float amount) {
        CurrentCargo = amount;
        Debug.Log("Poprawnie przypisano " + amount + " Cargo");
    }
    public void SetMaxHP(float amount) {
        MaxHP = amount;
        Debug.Log("Poprawnie przypisano " + amount + " MaxHP");
    }
    public void SetEnergy(float amount) {
        CurrentEnergy = amount;
        Debug.Log("Poprawnie przypisano " + amount + " Paliwa");
    }
    public void SetMaxEnergy(float amount) {
        MaxEnergy = amount;
        Debug.Log("Poprawnie przypisano " + amount + " MaxPaliwa");
    }


    public void SetHPCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            // Parsowanie ze stringa na inta, jak nie jest liczba po słowie kluczowym, idzie do else
            if (Int32.TryParse(args[0], out amount)) {
                SetHP(amount);
            }
            else {
                Debug.Log("Coś poszło nie tak, źle wpisałeś komende");
            }
        }
    }
    public void SetMaxHPCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetMaxHP(amount);
            }
            else {
                Debug.Log("Coś poszło nie tak, źle wpisałeś komende");
            }
        }
    }
    public void SetEnergyCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetEnergy(amount);
            }
            else {
                Debug.Log("Coś poszło nie tak, źle wpisałeś komende");
            }
        }
    }
    public void SetMaxEnergyCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetMaxEnergy(amount);
            }
            else {
                Debug.Log("Coś poszło nie tak, źle wpisałeś komende");
            }
        }
    }
    public void GetHPCommand(string[] args) {
        Debug.Log("Aktualny stan HP wynosi: " + CurrentHP + "/" + MaxHP);
    }
    public void GetEnergyCommand(string[] args) {
        Debug.Log("Aktualny stan paliwa wynosi: " + CurrentEnergy + "/" + MaxEnergy);
    }

    public List<string> GetUnlockedUpgradesList()
    {
        return purchasedUpgrades;
    }

    public void UnlockUpgrade(string upgradeID)
    {
        if (!purchasedUpgrades.Contains(upgradeID))
        {
            purchasedUpgrades.Add(upgradeID);
            Debug.Log("Odblokowano ulepszenie: " + upgradeID);
        }
    }
    public void LoadUpgrades(List<string> upgrades)
    {
        if (upgrades == null) return;

        purchasedUpgrades = new List<string>(upgrades);
        Debug.Log("Wczytano ulepszenia w ShipStats: " + purchasedUpgrades.Count);
    }

    public float GetMaxHP() { return MaxHP; }
    public float GetMaxEnergy() { return MaxEnergy; }
    public float GetMaxCargo() { return MaxCargo; }
}