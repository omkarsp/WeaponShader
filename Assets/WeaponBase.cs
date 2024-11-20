using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Base weapon class defining core functionality
public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected ParticleSystem weaponVFX;
    [SerializeField] protected Material weaponMaterial;
    [SerializeField] protected float damage;
    [SerializeField] protected float cooldown;

    protected bool canFire = true;
    protected float cooldownTimer;

    public abstract void Fire();
    public abstract void UpdateWeaponEffect();

    protected virtual void Start()
    {
        if (weaponMaterial != null)
        {
            // Create instance of material to avoid shared materials
            weaponMaterial = new Material(weaponMaterial);
            GetComponent<Renderer>().material = weaponMaterial;
        }
    }

    protected virtual void Update()
    {
        if (!canFire)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= cooldown)
            {
                canFire = true;
                cooldownTimer = 0;
            }
        }
    }

    protected IEnumerator StartCooldown()
    {
        canFire = false;
        yield return new WaitForSeconds(cooldown);
        canFire = true;
    }
}

// Fire weapon implementation
public class FireWeapon : WeaponBase
{
    [SerializeField] private GameObject fireballPrefab;
    [SerializeField] private float fireballSpeed = 10f;

    private static readonly int EmissionIntensity = Shader.PropertyToID("_EmissionIntensity");
    private static readonly int FireColor = Shader.PropertyToID("_FireColor");

    public override void Fire()
    {
        if (!canFire) return;

        GameObject fireball = Instantiate(fireballPrefab, transform.position, transform.rotation);
        fireball.GetComponent<Rigidbody>().velocity = transform.forward * fireballSpeed;

        if (weaponVFX != null)
        {
            weaponVFX.Play();
        }

        StartCoroutine(StartCooldown());
    }

    public override void UpdateWeaponEffect()
    {
        if (weaponMaterial != null)
        {
            // Pulse the emission intensity
            float pulseValue = (Mathf.Sin(Time.time * 2f) + 1f) / 2f;
            weaponMaterial.SetFloat(EmissionIntensity, pulseValue * 2f);

            // Update fire color based on intensity
            Color fireColor = Color.Lerp(Color.yellow, Color.red, pulseValue);
            weaponMaterial.SetColor(FireColor, fireColor);
        }
    }
}

// Ice weapon implementation
public class IceWeapon : WeaponBase
{
    [SerializeField] private GameObject icePrefab;
    [SerializeField] private float freezeRadius = 5f;

    private static readonly int FrostAmount = Shader.PropertyToID("_FrostAmount");
    private static readonly int IceColor = Shader.PropertyToID("_IceColor");

    public override void Fire()
    {
        if (!canFire) return;

        // Create ice effect in an area
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, freezeRadius);
        foreach (Collider hitCollider in hitColliders)
        {
            // Apply freeze effect to affected objects
            IFreezable freezable = hitCollider.GetComponent<IFreezable>();
            freezable?.Freeze();
        }

        if (weaponVFX != null)
        {
            weaponVFX.Play();
        }

        StartCoroutine(StartCooldown());
    }

    public override void UpdateWeaponEffect()
    {
        if (weaponMaterial != null)
        {
            // Crystalline frost effect
            float frostValue = (Mathf.Sin(Time.time * 1.5f) + 1f) / 2f;
            weaponMaterial.SetFloat(FrostAmount, frostValue);

            // Ice color variation
            Color iceColor = Color.Lerp(Color.white, Color.cyan, frostValue);
            weaponMaterial.SetColor(IceColor, iceColor);
        }
    }
}

// Lightning weapon implementation
public class LightningWeapon : WeaponBase
{
    [SerializeField] private LineRenderer lightningBeam;
    [SerializeField] private float maxDistance = 20f;

    private static readonly int ElectricityIntensity = Shader.PropertyToID("_ElectricityIntensity");
    private static readonly int LightningColor = Shader.PropertyToID("_LightningColor");

    public override void Fire()
    {
        if (!canFire) return;

        StartCoroutine(FireLightningBeam());
    }

    private IEnumerator FireLightningBeam()
    {
        lightningBeam.enabled = true;

        float duration = 2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Update lightning beam positions
            Vector3 startPoint = transform.position;
            Vector3 endPoint = startPoint + transform.forward * maxDistance;

            // Raycast to check for obstacles
            if (Physics.Raycast(startPoint, transform.forward, out RaycastHit hit, maxDistance))
            {
                endPoint = hit.point;

                // Apply damage to hit object
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.TakeDamage(damage * Time.deltaTime);
            }

            // Update beam positions with slight randomization for lightning effect
            Vector3[] positions = new Vector3[2];
            positions[0] = startPoint;
            positions[1] = Vector3.Lerp(startPoint, endPoint, 0.8f) + Random.insideUnitSphere * 0.2f;
            lightningBeam.SetPositions(positions);

            elapsed += Time.deltaTime;
            yield return null;
        }

        lightningBeam.enabled = false;
        StartCoroutine(StartCooldown());
    }

    public override void UpdateWeaponEffect()
    {
        if (weaponMaterial != null)
        {
            // Electric arcing effect
            float electricValue = Mathf.Pow((Mathf.Sin(Time.time * 8f) + 1f) / 2f, 2f);
            weaponMaterial.SetFloat(ElectricityIntensity, electricValue);

            // Lightning color variation
            Color lightningColor = Color.Lerp(Color.blue, Color.white, electricValue);
            weaponMaterial.SetColor(LightningColor, lightningColor);
        }
    }
}

// Weapon manager to handle switching between weapons
public class WeaponManager : MonoBehaviour
{
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private UIWeaponSelector uiSelector;

    private WeaponBase currentWeapon;
    private int currentWeaponIndex;

    private void Start()
    {
        // Initialize with first weapon
        if (weapons.Length > 0)
        {
            SwitchWeapon(0);
        }
    }

    private void Update()
    {
        if (currentWeapon != null)
        {
            currentWeapon.UpdateWeaponEffect();

            // Handle input
            if (Input.GetMouseButtonDown(0))
            {
                currentWeapon.Fire();
            }
        }
    }

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length) return;

        // Disable current weapon
        if (currentWeapon != null)
        {
            currentWeapon.gameObject.SetActive(false);
        }

        // Enable new weapon
        currentWeaponIndex = index;
        currentWeapon = weapons[currentWeaponIndex];
        currentWeapon.gameObject.SetActive(true);

        // Update UI
        uiSelector?.UpdateSelectedWeapon(currentWeaponIndex);
    }
}

// UI Controller for weapon selection
public class UIWeaponSelector : MonoBehaviour
{
    [SerializeField] private Button[] weaponButtons;
    [SerializeField] private WeaponManager weaponManager;

    private void Start()
    {
        // Set up button listeners
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            int index = i; // Capture index for lambda
            weaponButtons[i].onClick.AddListener(() => weaponManager.SwitchWeapon(index));
        }
    }

    public void UpdateSelectedWeapon(int index)
    {
        // Update button visuals
        for (int i = 0; i < weaponButtons.Length; i++)
        {
            weaponButtons[i].interactable = i != index;
        }
    }
}

// Interfaces for interaction
public interface IDamageable
{
    void TakeDamage(float damage);
}

public interface IFreezable
{
    void Freeze();
}