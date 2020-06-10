using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MyCompany.Models.RelationshipTools;
using Newtonsoft.Json;

namespace MyCompany.Models {
    [Table("job")]
    public class Job {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        public string JobTitle { get; set; }

        public long? MinSalary { get; set; }

        public long? MaxSalary { get; set; }

        [JsonIgnore]
        public IList<JobChore> JobChores { get; } = new List<JobChore>();

        [NotMapped]
        public IList<PieceOfWork> Chores { get; }

        public Employee Employee { get; set; }

        // jhipster-needle-entity-add-field - JHipster will add fields here, do not remove

        public Job()
        {
            Chores = new JoinListFacade<PieceOfWork, Job, JobChore>(this, JobChores);
        }

        public override bool Equals(object obj)
        {
            if (this == obj) return true;
            if (obj == null || GetType() != obj.GetType()) return false;
            var job = obj as Job;
            if (job?.Id == null || job?.Id == 0 || Id == 0) return false;
            return EqualityComparer<long>.Default.Equals(Id, job.Id);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }

        public override string ToString()
        {
            return "Job{" +
                    $"ID='{Id}'" +
                    $", JobTitle='{JobTitle}'" +
                    $", MinSalary='{MinSalary}'" +
                    $", MaxSalary='{MaxSalary}'" +
                    "}";
        }
    }
}
