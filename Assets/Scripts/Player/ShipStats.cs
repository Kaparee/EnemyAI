using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;

// Przechowuje statystyki i stan statku (HP, pancerz, osłony).
// Przygotowane w taki sposób, aby łatwo było to podpinać pod UI i system otrzymywania obrażeń (DamageCollision).
public class ShipStats : MonoBehaviour {
    public static event Action<Vector3, float, bool> OnDamageDealt;

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
    [SerializeField] private float maneuverForce = 24000f;
    [SerializeField] private float rollForce = 24000f;
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

    // Wyrownuje maksymalne pule zdrowia oraz energii jezeli startowe wartosci okazaly sie byc niewlasciwie nadpisane w edytorze.
    public void Start() {

        if (CurrentHP <= 0) CurrentHP = MaxHP;
        if (CurrentEnergy <= 0) CurrentEnergy = MaxEnergy;
        if (CurrentCargo <= 0) CurrentCargo = 0;
    }

    // Cofa pelna ilosc zapasow i calkowity stan techniczny kadluba przywracajac stan rownowagi poczatkowej instancji.
    public void ResetData()
    {
        CurrentHP = MaxHP;
        CurrentEnergy = MaxEnergy;
        CurrentCargo = 0;
    }

    // Oblicza nowa maksymalna pojemnosc ladowni poszerzajac limit mnozac baze rdzenna o wprowadzony wykladnik zwiekszenia obciazenia.
    public void UpdateMaxCargo(float multiplier) 
    {
        MaxCargo = baseCargoCapacity * multiplier;
        Debug.Log($"[ShipStats] Zwiększono pojemność ładowni do {MaxCargo} ton.");
    }

    // Kalkuluje redukcje otrzymanych ran na pancerzu uwzgledniajac kat kierunkowy uderzenia i mnozniki dla oslon zaleznie od strefy narazonej.
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

    // Odejmuje zweryfikowana ilosc wytrzymalosci z calkowitego HP, oglasza zdarzenie i w razie smierci wykonuje ostateczny skrypt wylaczania.
    public void TakeDamage(float damage)
    {
        if (damage > 0f)
        {
            CurrentHP = CurrentHP - damage;
            OnDamageDealt?.Invoke(transform.position, damage, CompareTag("Player"));
            Debug.Log("Otrzymano obrażenia! HP wynosi teraz: " + CurrentHP);
            if (CurrentHP <= 0f)
            {
                CurrentHP = 0f;
                if (!IsDestroyed)
                {
                    IsDestroyed = true;
                    HandleDestruction();
                }
                Debug.Log("Statek został całkowicie zniszczony.");
            }
        }
        else
        {
            Debug.Log("Wartość otrzymanych obrażeń musi być dodatnia.");
        }
    }

    // Deleguje zachowanie destrukcyjne jednostki bezposrednio do kontrolerow wrogow albo powiadamia globalna szyne o koncu kariery gracza.
    private void HandleDestruction()
    {
        Debug.Log("<color=red>[ShipStats] Statek zniszczony!</color>");

        EnemyAI enemyAI = GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.Die();
        }
        else
        {
            EventBus.TriggerOnPlayerDeath();
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ChangeState(GameState.GameOver);
            }
        }
    }

    // Przywraca integralnosc poszycia doliczajac podane wzmocnienie do ogolnej puli blokujac powrot smierci i wyjscie poza ramowy limit.
    public void Heal(float amount) {
        if (amount > 0f) {
            if (CurrentHP + amount > MaxHP) {
                CurrentHP = MaxHP;
                Debug.Log("HP przywrócone do maksymalnego poziomu.");
                IsDestroyed = false;
            }
            else {
                CurrentHP += amount;
                IsDestroyed = false;
            }
            Debug.Log("Naprawiono poszycie. Aktualne HP: " + CurrentHP);
        }
        else {
            Debug.Log("Wartość leczenia musi być dodatnia.");
        }
    }

    // Kosztuje zapas generatorow o wyslana ilosc zmniejszajac energie baku do zera po kompletnym wyczerpaniu nosnika pradu.
    public void UseEnergy(float amount) {
        if (amount > 0f) {
            if (CurrentEnergy < amount) {
                CurrentEnergy = 0;
                Debug.Log("Brak paliwa!");
            }
            else {
                CurrentEnergy -= amount;
            }
        }
        else {
            Debug.Log("Zużycie energii/paliwa musi być wartością dodatnią.");
        }
    }

    // Dotankowuje magazyny wezla podbiciem wskazanej objetosci limitujac ja bezpiecznie od gory bariera pojemnosci maski konstrukcji statku.
    public void AddEnergy(float amount) {
        if (amount > 0f) {
            if (CurrentEnergy + amount > MaxEnergy) {
                CurrentEnergy = MaxEnergy;
                Debug.Log("Paliwo zatankowane do pełna.");
            }
            else {
                CurrentEnergy += amount;
            }
            Debug.Log("Uzyskano paliwo. Aktualny stan: " + CurrentEnergy);
        }
        else {
            Debug.Log("Wartość tankowania musi być dodatnia.");
        }
    }

    // Probuje dodac ladunek do ekwipunku zwracajac flage potwierdzenia sukcesu jesli zaplanowana objetosc zmiesci sie w wolnych sekcjach ladowni.
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

    // Przelicza wage strukturalna modelu powiekaszajac ja o ciezar wewnetrznych zbiorow przydatna w estymacji dynamiki bryly statku.
    public float GetTotalMass() {
        return BaseMass + CurrentCargo;
    }

    // Sztywnie forsuje stan poszycia komenda uzytkownika narzucajac wprost rzadana wielkosc na lokalna pule zycia statku.
    public void SetHP(float amount) {
        CurrentHP = amount;
        Debug.Log("Zastosowano kod na HP: " + amount);
    }
    // Konfiguruje objetosc zaladunkowa z pominieciem naturalnych sprawdzianow bezpieczenstwa jako czesc narzedzia.
    public void SetCargo(float amount) {
        CurrentCargo = amount;
        Debug.Log("Kod na " + amount + " Cargo wpisany");
    }
    // Skaluje maksymalna gorna bariere zywotnosci calego mechanizmu statku pomagajac dynamicznie ulepszac parametry.
    public void SetMaxHP(float amount) {
        MaxHP = amount;
        Debug.Log("Kod na " + amount + " MaxHP wpisany");
    }
    // Twardo zamienia liczbe posiadanych megawatogodzin narzucajac nowy pulap pod konsole dla testow paliwa.
    public void SetEnergy(float amount) {
        CurrentEnergy = amount;
        Debug.Log("Zastosowano kod na paliwo: " + amount);
    }
    // Przebudowuje i utrwala nowa maksymalna pojemnosc dla baku narzucajac rozszerzony potencjal operacyjny.
    public void SetMaxEnergy(float amount) {
        MaxEnergy = amount;
        Debug.Log("Zastosowano kod na maksymalne paliwo: " + amount);
    }

    // Parsuje zestaw wejsciowych ciagow znakow probujac przetlumaczyc pierwsza komorke jako instrukcje przypisania zdrowia.
    public void SetHPCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetHP(amount);
            }
            else {
                Debug.Log("Nieprawidłowa wartość argumentu. Użyj liczby.");
            }
        }
    }
    // Czyta tekstowe polecenie z parametru a nastepnie wdraza zrzutowanie wyniku jako nowy limit punktow zycia.
    public void SetMaxHPCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetMaxHP(amount);
            }
            else {
                Debug.Log("Nieprawidłowa wartość argumentu. Użyj liczby.");
            }
        }
    }
    // Mapuje string w liczbe aby wprowadzic dyrektywe ustalajaca bezwglednie paliwo poprzez konsole uzytkownika.
    public void SetEnergyCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetEnergy(amount);
            }
            else {
                Debug.Log("Nieprawidłowa wartość argumentu. Użyj liczby.");
            }
        }
    }
    // Waliduje ilosc elementow argumentowych konwertujac rzadana liczbe w celu ustalenia zdatnosci maksimum baku.
    public void SetMaxEnergyCommand(string[] args) {
        if (args.Length > 0) {
            int amount = 0;
            if (Int32.TryParse(args[0], out amount)) {
                SetMaxEnergy(amount);
            }
            else {
                Debug.Log("Nieprawidłowa wartość argumentu. Użyj liczby.");
            }
        }
    }
    // Przesyla log biezacego stanu uszkodzen oraz pelnej zywotnosci podmiotu dla konsoli celem weryfikacji dzialan.
    public void GetHPCommand(string[] args) {
        Debug.Log("Status statku (HP): " + CurrentHP + "/" + MaxHP);
    }
    // Wypisuje aktualna sytuacje zapasowa napedu zestawiona z rdzennym punktem mozliwosci calkowitego zaopatrzenia.
    public void GetEnergyCommand(string[] args) {
        Debug.Log("Status statku (Paliwo): " + CurrentEnergy + "/" + MaxEnergy);
    }

    // Udostepnia do wgladu rejestr nabytych badz znalezionych usprawnien w postaci zbiorczej struktury uzytecznej globalnie.
    public List<string> GetUnlockedUpgradesList()
    {
        return purchasedUpgrades;
    }

    // Indeksuje modyfikacje wprowadzajac ja jako zasymilowana o ile wpis nie figuruje jako duplikat w globalnej macierzy jednostki.
    public void UnlockUpgrade(string upgradeID)
    {
        if (!purchasedUpgrades.Contains(upgradeID))
        {
            purchasedUpgrades.Add(upgradeID);
            Debug.Log("Odblokowano ulepszenie: " + upgradeID);
        }
    }
    // Odswieza zestaw wyposazenia klonujac na swiezo odtworzona kopie powierzonych informacji modyfikacji po restarcie gry.
    public void LoadUpgrades(List<string> upgrades)
    {
        if (upgrades == null) return;

        purchasedUpgrades = new List<string>(upgrades);
        Debug.Log("Załadowano ulepszenia: " + purchasedUpgrades.Count + " szt.");
    }

    // Zwraca wartosc maksymalnych punktow wytrzymalosci jednostki.
    public float GetMaxHP() { return MaxHP; }
    // Zwraca wartosc maksymalnej energii dostepnej dla napedu jednostki.
    public float GetMaxEnergy() { return MaxEnergy; }
    // Zwraca wartosc maksymalnej ladownosci inwentarza jednostki.
    public float GetMaxCargo() { return MaxCargo; }
}