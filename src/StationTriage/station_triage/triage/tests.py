import json
from django.test import TestCase, Client
from django.urls import reverse
from .models import ShipAssessment


def make_ship(**kwargs):
    defaults = {
        "ship_name": "Nebula Runner",
        "callsign": "NR-007",
        "captain_name": "Han Solo",
        "cargo_items": ["spice"],
        "passengers": ["Chewie"],
        "biohazard_level": 0,
        "chemical_hazard_level": 0,
        "security_hazard_level": 0,
        "recommendation": "All clear.",
    }
    defaults.update(kwargs)
    return ShipAssessment.objects.create(**defaults)


class TriageViewTests(TestCase):

    # --- Security queue ---

    def test_security_queue_returns_only_ships_with_nonzero_security_hazard_level(self):
        make_ship(callsign="A", security_hazard_level=3)
        make_ship(callsign="B", security_hazard_level=0)

        response = self.client.get("/security/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertIn("A", callsigns)
        self.assertNotIn("B", callsigns)

    def test_security_queue_excludes_resolved_ships(self):
        make_ship(callsign="RESOLVED", security_hazard_level=5, security_status=ShipAssessment.Status.RESOLVED)
        make_ship(callsign="ACTIVE", security_hazard_level=5, security_status=ShipAssessment.Status.NEW)

        response = self.client.get("/security/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertNotIn("RESOLVED", callsigns)
        self.assertIn("ACTIVE", callsigns)

    def test_security_queue_orders_by_security_hazard_level_descending(self):
        make_ship(callsign="LOW", security_hazard_level=2)
        make_ship(callsign="HIGH", security_hazard_level=8)
        make_ship(callsign="MID", security_hazard_level=5)

        response = self.client.get("/security/")

        data = json.loads(response.content)
        levels = [s["security_hazard_level"] for s in data]
        self.assertEqual(levels, sorted(levels, reverse=True))

    # --- Medical queue ---

    def test_medical_queue_returns_only_ships_with_nonzero_biohazard_level(self):
        make_ship(callsign="BIO", biohazard_level=4)
        make_ship(callsign="CLEAN", biohazard_level=0)

        response = self.client.get("/medical/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertIn("BIO", callsigns)
        self.assertNotIn("CLEAN", callsigns)

    def test_medical_queue_excludes_resolved_ships(self):
        make_ship(callsign="MEDRESOLVED", biohazard_level=3, medical_status=ShipAssessment.Status.RESOLVED)
        make_ship(callsign="MEDACTIVE", biohazard_level=3, medical_status=ShipAssessment.Status.NEW)

        response = self.client.get("/medical/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertNotIn("MEDRESOLVED", callsigns)
        self.assertIn("MEDACTIVE", callsigns)

    def test_medical_queue_orders_by_biohazard_level_descending(self):
        make_ship(callsign="BIOLOW", biohazard_level=1)
        make_ship(callsign="BIOHIGH", biohazard_level=9)
        make_ship(callsign="BIOMID", biohazard_level=5)

        response = self.client.get("/medical/")

        data = json.loads(response.content)
        levels = [s["biohazard_level"] for s in data]
        self.assertEqual(levels, sorted(levels, reverse=True))

    # --- Hazmat queue ---

    def test_hazmat_queue_returns_only_ships_with_nonzero_chemical_hazard_level(self):
        make_ship(callsign="CHEM", chemical_hazard_level=6)
        make_ship(callsign="SAFE", chemical_hazard_level=0)

        response = self.client.get("/hazmat/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertIn("CHEM", callsigns)
        self.assertNotIn("SAFE", callsigns)

    def test_hazmat_queue_excludes_resolved_ships(self):
        make_ship(callsign="HAZRESOLVED", chemical_hazard_level=4, hazmat_status=ShipAssessment.Status.RESOLVED)
        make_ship(callsign="HAZACTIVE", chemical_hazard_level=4, hazmat_status=ShipAssessment.Status.NEW)

        response = self.client.get("/hazmat/")

        data = json.loads(response.content)
        callsigns = [s["callsign"] for s in data]
        self.assertNotIn("HAZRESOLVED", callsigns)
        self.assertIn("HAZACTIVE", callsigns)

    def test_hazmat_queue_orders_by_chemical_hazard_level_descending(self):
        make_ship(callsign="CHEMLOW", chemical_hazard_level=2)
        make_ship(callsign="CHEMHIGH", chemical_hazard_level=7)
        make_ship(callsign="CHEMMID", chemical_hazard_level=4)

        response = self.client.get("/hazmat/")

        data = json.loads(response.content)
        levels = [s["chemical_hazard_level"] for s in data]
        self.assertEqual(levels, sorted(levels, reverse=True))

    # --- Security detail GET ---

    def test_security_detail_get_returns_correct_ship_data(self):
        ship = make_ship(callsign="DETAIL-001", security_hazard_level=5)

        response = self.client.get(f"/security/{ship.id}/")

        self.assertEqual(response.status_code, 200)
        data = json.loads(response.content)
        self.assertEqual(data["callsign"], "DETAIL-001")
        self.assertEqual(data["security_hazard_level"], 5)
        self.assertIn("recommendation", data)
        self.assertIn("cargo_items", data)
        self.assertIn("passengers", data)

    def test_security_detail_get_returns_404_for_unknown_id(self):
        response = self.client.get("/security/999999/")

        self.assertEqual(response.status_code, 404)

    # --- Security detail POST ---

    def test_security_detail_post_with_valid_status_updates_security_status(self):
        ship = make_ship(callsign="UPDATE-SEC", security_hazard_level=3)

        response = self.client.post(
            f"/security/{ship.id}/",
            data=json.dumps({"security_status": "IN_PROGRESS"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 200)
        data = json.loads(response.content)
        self.assertTrue(data["ok"])
        ship.refresh_from_db()
        self.assertEqual(ship.security_status, ShipAssessment.Status.IN_PROGRESS)

    def test_security_detail_post_with_invalid_status_returns_400(self):
        ship = make_ship(callsign="BADSEC", security_hazard_level=3)

        response = self.client.post(
            f"/security/{ship.id}/",
            data=json.dumps({"security_status": "EXPLODE"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)

    def test_security_detail_post_with_missing_status_returns_400(self):
        ship = make_ship(callsign="MISSINGSEC", security_hazard_level=3)

        response = self.client.post(
            f"/security/{ship.id}/",
            data=json.dumps({}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)

    # --- Medical detail ---

    def test_medical_detail_get_returns_correct_ship_data(self):
        ship = make_ship(callsign="MEDDETAIL", biohazard_level=4)

        response = self.client.get(f"/medical/{ship.id}/")

        self.assertEqual(response.status_code, 200)
        data = json.loads(response.content)
        self.assertEqual(data["callsign"], "MEDDETAIL")
        self.assertEqual(data["biohazard_level"], 4)

    def test_medical_detail_get_returns_404_for_unknown_id(self):
        response = self.client.get("/medical/999999/")

        self.assertEqual(response.status_code, 404)

    def test_medical_detail_post_with_valid_status_updates_medical_status(self):
        ship = make_ship(callsign="UPDATEMED", biohazard_level=2)

        response = self.client.post(
            f"/medical/{ship.id}/",
            data=json.dumps({"medical_status": "RESOLVED"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(json.loads(response.content)["ok"])
        ship.refresh_from_db()
        self.assertEqual(ship.medical_status, ShipAssessment.Status.RESOLVED)

    def test_medical_detail_post_with_invalid_status_returns_400(self):
        ship = make_ship(callsign="BADMED", biohazard_level=2)

        response = self.client.post(
            f"/medical/{ship.id}/",
            data=json.dumps({"medical_status": "INVALID"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)

    def test_medical_detail_post_with_missing_status_returns_400(self):
        ship = make_ship(callsign="MISSINGMED", biohazard_level=2)

        response = self.client.post(
            f"/medical/{ship.id}/",
            data=json.dumps({}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)

    # --- Hazmat detail ---

    def test_hazmat_detail_get_returns_correct_ship_data(self):
        ship = make_ship(callsign="HAZDETAIL", chemical_hazard_level=6)

        response = self.client.get(f"/hazmat/{ship.id}/")

        self.assertEqual(response.status_code, 200)
        data = json.loads(response.content)
        self.assertEqual(data["callsign"], "HAZDETAIL")
        self.assertEqual(data["chemical_hazard_level"], 6)

    def test_hazmat_detail_get_returns_404_for_unknown_id(self):
        response = self.client.get("/hazmat/999999/")

        self.assertEqual(response.status_code, 404)

    def test_hazmat_detail_post_with_valid_status_updates_hazmat_status(self):
        ship = make_ship(callsign="UPDATEHAZ", chemical_hazard_level=5)

        response = self.client.post(
            f"/hazmat/{ship.id}/",
            data=json.dumps({"hazmat_status": "IN_PROGRESS"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 200)
        self.assertTrue(json.loads(response.content)["ok"])
        ship.refresh_from_db()
        self.assertEqual(ship.hazmat_status, ShipAssessment.Status.IN_PROGRESS)

    def test_hazmat_detail_post_with_invalid_status_returns_400(self):
        ship = make_ship(callsign="BADHAZ", chemical_hazard_level=5)

        response = self.client.post(
            f"/hazmat/{ship.id}/",
            data=json.dumps({"hazmat_status": "KABOOM"}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)

    def test_hazmat_detail_post_with_missing_status_returns_400(self):
        ship = make_ship(callsign="MISSINGHAZ", chemical_hazard_level=5)

        response = self.client.post(
            f"/hazmat/{ship.id}/",
            data=json.dumps({}),
            content_type="application/json",
        )

        self.assertEqual(response.status_code, 400)
