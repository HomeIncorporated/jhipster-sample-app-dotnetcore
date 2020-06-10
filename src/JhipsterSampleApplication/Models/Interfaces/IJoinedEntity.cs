namespace MyCompany.Models.Interfaces {
    public interface IJoinedEntity<TEntity> {
        TEntity Join { get; set; }
    }
}
