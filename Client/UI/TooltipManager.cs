using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RealmOfReality.Client.Engine;
using RealmOfReality.Shared.Items;

namespace RealmOfReality.Client.UI;

/// <summary>
/// Manages rich tooltips for items, skills, and other game elements
/// </summary>
public class TooltipManager
{
    private readonly UIRenderer _ui;
    private readonly InputManager _input;
    
    // Current tooltip state
    private TooltipData? _currentTooltip;
    private double _hoverTime = 0;
    private const double HoverDelay = 0.15; // Delay before showing tooltip
    private Vector2 _lastMousePos;
    private bool _isVisible = false;
    
    public TooltipManager(UIRenderer ui, InputManager input)
    {
        _ui = ui;
        _input = input;
    }
    
    public void SetTooltip(TooltipData? tooltip)
    {
        if (tooltip == null)
        {
            _currentTooltip = null;
            _isVisible = false;
            _hoverTime = 0;
            return;
        }
        
        if (_currentTooltip?.Id != tooltip.Id)
        {
            _currentTooltip = tooltip;
            _hoverTime = 0;
            _isVisible = false;
        }
    }
    
    public void SetItemTooltip(Item? item, Equipment? equippedItems = null)
    {
        if (item == null)
        {
            SetTooltip(null);
            return;
        }
        
        var tooltip = CreateItemTooltip(item, equippedItems);
        SetTooltip(tooltip);
    }
    
    public void Update(GameTime gameTime)
    {
        var mousePos = _input.MousePosition;
        
        // Reset if mouse moved significantly
        if (Vector2.Distance(mousePos, _lastMousePos) > 5)
        {
            _hoverTime = 0;
            _isVisible = false;
        }
        
        _lastMousePos = mousePos;
        
        if (_currentTooltip != null)
        {
            _hoverTime += gameTime.ElapsedGameTime.TotalSeconds;
            if (_hoverTime >= HoverDelay)
            {
                _isVisible = true;
            }
        }
    }
    
    public void Draw(SpriteBatch spriteBatch)
    {
        if (!_isVisible || _currentTooltip == null) return;
        
        var mousePos = _input.MousePosition;
        var tooltip = _currentTooltip;
        
        // Calculate tooltip dimensions
        int maxWidth = 280;
        int lineHeight = 16;
        int headerHeight = 22;
        int padding = 10;
        
        // Count lines
        int lines = 0;
        if (!string.IsNullOrEmpty(tooltip.Type)) lines++;
        if (tooltip.RequiredLevel > 0) lines++;
        if (tooltip.Damage != null) lines++;
        if (tooltip.Armor > 0) lines++;
        if (tooltip.Speed > 0) lines++;
        if (tooltip.DPS > 0) lines++;
        lines += tooltip.Stats.Count;
        lines += tooltip.Effects.Count;
        if (tooltip.Durability != null) lines++;
        if (!string.IsNullOrEmpty(tooltip.FlavorText)) lines += 2;
        if (!string.IsNullOrEmpty(tooltip.UseText)) lines++;
        
        // Calculate height
        int height = padding * 2 + headerHeight + lines * lineHeight + 8;
        int width = maxWidth;
        
        // Comparison section
        if (tooltip.ComparisonStats.Count > 0)
        {
            height += 20 + tooltip.ComparisonStats.Count * lineHeight;
        }
        
        // Position tooltip
        int tooltipX = (int)mousePos.X + 15;
        int tooltipY = (int)mousePos.Y + 15;
        
        if (tooltipX + width > 1280) tooltipX = (int)mousePos.X - width - 15;
        if (tooltipY + height > 720) tooltipY = 720 - height;
        
        var rect = new Rectangle(tooltipX, tooltipY, width, height);
        
        // Draw background with gradient
        DrawTooltipBackground(rect, tooltip.RarityColor);
        
        // Draw content
        int y = tooltipY + padding;
        
        // Title
        _ui.DrawText(tooltip.Name, new Vector2(tooltipX + padding, y), tooltip.RarityColor, 1.5f);
        y += headerHeight;
        
        // Type (if any)
        if (!string.IsNullOrEmpty(tooltip.Type))
        {
            _ui.DrawText(tooltip.Type, new Vector2(tooltipX + padding, y), Color.Gray, 1.1f);
            y += lineHeight;
        }
        
        // Separator
        _ui.DrawRectangle(new Rectangle(tooltipX + padding, y, width - padding * 2, 1), new Color(80, 80, 100));
        y += 6;
        
        // Required level
        if (tooltip.RequiredLevel > 0)
        {
            var levelColor = tooltip.CanEquip ? Color.White : new Color(255, 100, 100);
            var levelText = tooltip.CanEquip 
                ? $"Requires Level {tooltip.RequiredLevel}" 
                : $"Requires Level {tooltip.RequiredLevel} (Too Low!)";
            _ui.DrawText(levelText, new Vector2(tooltipX + padding, y), levelColor, 1.1f);
            y += lineHeight;
        }
        
        // Main stats
        if (tooltip.Damage != null)
        {
            _ui.DrawText($"Damage: {tooltip.Damage}", new Vector2(tooltipX + padding, y), Color.White, 1.2f);
            y += lineHeight;
        }
        
        if (tooltip.Armor > 0)
        {
            _ui.DrawText($"Armor: {tooltip.Armor}", new Vector2(tooltipX + padding, y), Color.White, 1.2f);
            y += lineHeight;
        }
        
        if (tooltip.Speed > 0)
        {
            _ui.DrawText($"Speed: {tooltip.Speed:F1}", new Vector2(tooltipX + padding, y), Color.White, 1.1f);
            y += lineHeight;
        }
        
        if (tooltip.DPS > 0)
        {
            _ui.DrawText($"({tooltip.DPS:F1} damage per second)", new Vector2(tooltipX + padding, y), Color.Gray, 1f);
            y += lineHeight;
        }
        
        // Bonus stats
        foreach (var stat in tooltip.Stats)
        {
            var color = stat.Value > 0 ? new Color(100, 255, 100) : new Color(255, 100, 100);
            var sign = stat.Value > 0 ? "+" : "";
            _ui.DrawText($"{sign}{stat.Value} {stat.Name}", new Vector2(tooltipX + padding, y), color, 1.1f);
            y += lineHeight;
        }
        
        // Special effects
        if (tooltip.Effects.Count > 0)
        {
            y += 4;
            foreach (var effect in tooltip.Effects)
            {
                _ui.DrawText($"• {effect}", new Vector2(tooltipX + padding, y), new Color(100, 200, 255), 1.1f);
                y += lineHeight;
            }
        }
        
        // Durability
        if (tooltip.Durability != null)
        {
            y += 4;
            var durColor = tooltip.DurabilityPercent > 25 ? Color.Gray : new Color(255, 150, 50);
            _ui.DrawText($"Durability: {tooltip.Durability}", new Vector2(tooltipX + padding, y), durColor, 1f);
            y += lineHeight;
        }
        
        // Comparison section
        if (tooltip.ComparisonStats.Count > 0)
        {
            y += 8;
            _ui.DrawRectangle(new Rectangle(tooltipX + padding, y, width - padding * 2, 1), new Color(80, 80, 100));
            y += 6;
            
            _ui.DrawText("Compared to equipped:", new Vector2(tooltipX + padding, y), Color.Gray, 1f);
            y += lineHeight;
            
            foreach (var comp in tooltip.ComparisonStats)
            {
                var color = comp.Difference > 0 ? new Color(100, 255, 100) : new Color(255, 100, 100);
                var sign = comp.Difference > 0 ? "+" : "";
                var arrow = comp.Difference > 0 ? "▲" : "▼";
                _ui.DrawText($"  {arrow} {sign}{comp.Difference} {comp.Name}", new Vector2(tooltipX + padding, y), color, 1.1f);
                y += lineHeight;
            }
        }
        
        // Flavor text
        if (!string.IsNullOrEmpty(tooltip.FlavorText))
        {
            y += 8;
            _ui.DrawText($"\"{tooltip.FlavorText}\"", new Vector2(tooltipX + padding, y), new Color(255, 220, 100), 1f);
            y += lineHeight * 2;
        }
        
        // Use text
        if (!string.IsNullOrEmpty(tooltip.UseText))
        {
            _ui.DrawText(tooltip.UseText, new Vector2(tooltipX + padding, y), new Color(150, 255, 150), 1f);
        }
    }
    
    private void DrawTooltipBackground(Rectangle rect, Color rarityColor)
    {
        // Main background
        _ui.DrawRectangle(rect, new Color(20, 20, 30, 245));
        
        // Rarity accent bar at top
        var accentRect = new Rectangle(rect.X, rect.Y, rect.Width, 3);
        _ui.DrawRectangle(accentRect, rarityColor);
        
        // Border
        _ui.DrawRectangleOutline(rect, new Color(60, 60, 80));
        
        // Inner highlight
        var innerRect = new Rectangle(rect.X + 1, rect.Y + 3, rect.Width - 2, rect.Height - 4);
        _ui.DrawRectangleOutline(innerRect, new Color(40, 40, 60));
    }
    
    private TooltipData CreateItemTooltip(Item item, Equipment? equippedItems)
    {
        var def = item.Definition;
        var tooltip = new TooltipData
        {
            Id = $"item_{item.Id}",
            Name = item.Name,
            RarityColor = GetRarityColor(def?.Rarity ?? ItemRarity.Common)
        };
        
        if (def != null)
        {
            // Type
            tooltip.Type = GetItemTypeString(def);
            
            // Equipment requirements
            if (def.RequiredLevel > 0)
            {
                tooltip.RequiredLevel = def.RequiredLevel;
                tooltip.CanEquip = true; // Would check player level
            }
            
            // Weapon stats
            if (def.MinDamage > 0 || def.MaxDamage > 0)
            {
                tooltip.Damage = $"{def.MinDamage} - {def.MaxDamage}";
                if (def.AttackSpeed > 0)
                {
                    tooltip.Speed = def.AttackSpeed;
                    var avgDamage = (def.MinDamage + def.MaxDamage) / 2f;
                    tooltip.DPS = avgDamage * def.AttackSpeed;
                }
            }
            
            // Armor
            if (def.Armor > 0)
            {
                tooltip.Armor = def.Armor;
            }
            
            // Bonus stats
            if (def.BonusStrength > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Strength", Value = def.BonusStrength });
            if (def.BonusDexterity > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Dexterity", Value = def.BonusDexterity });
            if (def.BonusIntelligence > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Intelligence", Value = def.BonusIntelligence });
            if (def.BonusHealth > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Health", Value = def.BonusHealth });
            if (def.BonusMana > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Mana", Value = def.BonusMana });
            if (def.BonusStamina > 0)
                tooltip.Stats.Add(new StatInfo { Name = "Stamina", Value = def.BonusStamina });
            
            // Special effects from flags
            if ((def.Flags & ItemFlags.Magic) != 0)
                tooltip.Effects.Add("Magical Item");
            if ((def.Flags & ItemFlags.Blessed) != 0)
                tooltip.Effects.Add("Blessed - Protected from loss on death");
            if ((def.Flags & ItemFlags.Cursed) != 0)
                tooltip.Effects.Add("Cursed - Cannot be unequipped");
            if ((def.Flags & ItemFlags.Soulbound) != 0)
                tooltip.Effects.Add("Soulbound - Cannot be traded");
            
            // Use text for consumables
            if (def.Category == ItemCategory.Consumable || (def.Flags & ItemFlags.Consumable) != 0)
            {
                if (def.HealAmount > 0)
                    tooltip.UseText = $"Right-click to restore {def.HealAmount} health";
                else if (def.ManaAmount > 0)
                    tooltip.UseText = $"Right-click to restore {def.ManaAmount} mana";
                else
                    tooltip.UseText = "Right-click to use";
            }
            else if ((def.Flags & ItemFlags.Equipable) != 0 || def.Layer != Layer.Invalid)
            {
                tooltip.UseText = "Right-click to equip";
            }
            
            // Compare to equipped item
            if (equippedItems != null && def.Slot != EquipmentSlot.None)
            {
                var equipped = equippedItems[def.Slot];
                if (equipped?.Definition != null)
                {
                    var eDef = equipped.Definition;
                    
                    // Compare armor
                    if (def.Armor != 0 || eDef.Armor != 0)
                    {
                        var diff = def.Armor - eDef.Armor;
                        if (diff != 0)
                            tooltip.ComparisonStats.Add(new ComparisonInfo { Name = "Armor", Difference = diff });
                    }
                    
                    // Compare damage
                    if (def.MinDamage != 0 || eDef.MinDamage != 0)
                    {
                        var avgNew = (def.MinDamage + def.MaxDamage) / 2;
                        var avgOld = (eDef.MinDamage + eDef.MaxDamage) / 2;
                        var diff = avgNew - avgOld;
                        if (diff != 0)
                            tooltip.ComparisonStats.Add(new ComparisonInfo { Name = "Avg Damage", Difference = diff });
                    }
                    
                    // Compare stats
                    var strDiff = def.BonusStrength - eDef.BonusStrength;
                    if (strDiff != 0)
                        tooltip.ComparisonStats.Add(new ComparisonInfo { Name = "Strength", Difference = strDiff });
                    
                    var dexDiff = def.BonusDexterity - eDef.BonusDexterity;
                    if (dexDiff != 0)
                        tooltip.ComparisonStats.Add(new ComparisonInfo { Name = "Dexterity", Difference = dexDiff });
                    
                    var intDiff = def.BonusIntelligence - eDef.BonusIntelligence;
                    if (intDiff != 0)
                        tooltip.ComparisonStats.Add(new ComparisonInfo { Name = "Intelligence", Difference = intDiff });
                }
            }
        }
        
        // Durability
        if (item.MaxDurability > 0)
        {
            tooltip.Durability = $"{item.Durability}/{item.MaxDurability}";
            tooltip.DurabilityPercent = (int)(100f * item.Durability / item.MaxDurability);
        }
        
        // Stack count
        if (item.Amount > 1)
        {
            tooltip.Type = $"{tooltip.Type} (x{item.Amount})";
        }
        
        return tooltip;
    }
    
    private string GetItemTypeString(ItemDefinition def)
    {
        // Check by layer first for equipment
        var slot = def.Layer switch
        {
            Layer.Helm => "Head Armor",
            Layer.InnerTorso => "Chest Armor",
            Layer.MiddleTorso => "Tunic",
            Layer.OuterTorso => "Robe",
            Layer.Pants or Layer.InnerLegs => "Leg Armor",
            Layer.OuterLegs => "Skirt",
            Layer.Shoes => "Footwear",
            Layer.Gloves => "Gloves",
            Layer.Arms => "Arm Guards",
            Layer.Cloak => "Cloak",
            Layer.Necklace => "Necklace",
            Layer.Waist => "Belt",
            Layer.Ring or Layer.Bracelet => "Jewelry",
            Layer.OneHanded => "One-Hand Weapon",
            Layer.TwoHanded => (def.Flags & ItemFlags.TwoHanded) != 0 ? "Two-Hand Weapon" : "Shield/Off-Hand",
            Layer.Talisman => "Talisman",
            Layer.Hair => "Hair Style",
            Layer.FacialHair => "Facial Hair",
            _ => null
        };
        
        if (slot != null) return slot;
        
        return def.Category switch
        {
            ItemCategory.Consumable => "Consumable",
            ItemCategory.Material => "Crafting Material",
            ItemCategory.Tool => "Tool",
            ItemCategory.Weapon => "Weapon",
            ItemCategory.Armor => "Armor",
            ItemCategory.Accessory => "Jewelry",
            ItemCategory.Container => "Container",
            _ => "Item"
        };
    }
    
    private static Color GetRarityColor(ItemRarity rarity) => rarity switch
    {
        ItemRarity.Common => Color.White,
        ItemRarity.Uncommon => new Color(100, 255, 100),
        ItemRarity.Rare => new Color(100, 150, 255),
        ItemRarity.Epic => new Color(180, 100, 255),
        ItemRarity.Legendary => new Color(255, 180, 50),
        ItemRarity.Artifact => new Color(255, 215, 0),
        _ => Color.White
    };
}

public class TooltipData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Type { get; set; }
    public Color RarityColor { get; set; } = Color.White;
    
    public int RequiredLevel { get; set; }
    public bool CanEquip { get; set; } = true;
    
    public string? Damage { get; set; }
    public int Armor { get; set; }
    public float Speed { get; set; }
    public float DPS { get; set; }
    
    public List<StatInfo> Stats { get; } = new();
    public List<string> Effects { get; } = new();
    public List<ComparisonInfo> ComparisonStats { get; } = new();
    
    public string? Durability { get; set; }
    public int DurabilityPercent { get; set; } = 100;
    
    public string? FlavorText { get; set; }
    public string? UseText { get; set; }
}

public struct StatInfo
{
    public string Name;
    public int Value;
}

public struct ComparisonInfo
{
    public string Name;
    public int Difference;
}
