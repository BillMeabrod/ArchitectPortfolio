from django.db import models

class ShipAssessment(models.Model):
    # Ship manifest data
    ship_name = models.CharField(max_length=200)
    callsign = models.CharField(max_length=100)
    captain_name = models.CharField(max_length=200)
    cargo_items = models.JSONField()
    passengers = models.JSONField()

    # Risk assessment data
    biohazard_level = models.IntegerField()
    chemical_hazard_level = models.IntegerField()
    security_hazard_level = models.IntegerField()
    recommendation = models.TextField()

    # Triage status per role
    class Status(models.TextChoices):
        NEW = 'NEW', 'New'
        IN_PROGRESS = 'IN_PROGRESS', 'In Progress'
        RESOLVED = 'RESOLVED', 'Resolved'

    medical_status = models.CharField(max_length=20, choices=Status.choices, default=Status.NEW)
    hazmat_status = models.CharField(max_length=20, choices=Status.choices, default=Status.NEW)
    security_status = models.CharField(max_length=20, choices=Status.choices, default=Status.NEW)

    # Metadata
    received_at = models.DateTimeField(auto_now_add=True)

    class Meta:
        constraints = [
            models.CheckConstraint(
                name='biohazard_level_range',
                condition=models.Q(biohazard_level__range=(0, 10)),
            ),
            models.CheckConstraint(
                name='chemical_hazard_level_range',
                condition=models.Q(chemical_hazard_level__range=(0, 10)),
            ),
            models.CheckConstraint(
                name='security_hazard_level_range',
                condition=models.Q(security_hazard_level__range=(0, 10)),
            ),
        ]

    def __str__(self):
        return f"{self.callsign} - {self.ship_name}"