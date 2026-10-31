public class TavernWallet
{
    public int Coins { get; private set; } = 30;
    public bool Buy(int menu, int health, out int restored)
    {
        restored = 0;
        if (menu < 0 || menu > 1 || health <= 0 || health >= 5) return false;
        int cost = menu == 0 ? 5 : 10;
        if (Coins < cost) return false;
        restored = System.Math.Min(5 - health, menu == 0 ? 2 : 4);
        Coins -= cost;
        return true;
    }
    public void Reset() { Coins = 30; }
}
