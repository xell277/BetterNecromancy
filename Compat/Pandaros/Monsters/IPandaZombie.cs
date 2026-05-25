using AI;
using Monsters;

namespace Pandaros.API.Monsters
{
    public interface IPandaZombie : IMonster, IPandaDamage, IPandaArmor, INameable
    {
        float ZombieHPBonus { get; }
        string MosterType { get; }
        int MinColonists { get; }
        IPandaZombie GetNewInstance(Path path, Colony colony);
    }
}
