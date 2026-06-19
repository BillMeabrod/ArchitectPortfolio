import json
import logging
import os
import sys

import django

sys.path.insert(0, os.path.dirname(__file__))
os.environ.setdefault('DJANGO_SETTINGS_MODULE', 'station_triage.settings')
django.setup()

import azure.functions as func
from triage.models import ShipAssessment

app = func.FunctionApp()

@app.queue_trigger(
    arg_name="msg",
    queue_name="risk-assessment-queue",
    connection="AzureWebJobsStorage"
)
def process_risk_assessment(msg: func.QueueMessage):
    try:
        body = json.loads(msg.get_body().decode('utf-8'))
        manifest = body['Manifest']
        assessment = body['Assessment']

        ShipAssessment.objects.create(
            ship_name=manifest['ShipName'],
            callsign=manifest['Callsign'],
            captain_name=manifest['CaptainName'],
            cargo_items=manifest['CargoItems'],
            passengers=manifest['Passengers'],
            biohazard_level=assessment['BiohazardLevel'],
            chemical_hazard_level=assessment['ChemicalHazardLevel'],
            security_hazard_level=assessment['SecurityHazardLevel'],
            recommendation=assessment['Recommendation']
        )
        print(f"Created ShipAssessment for {manifest['Callsign']}")
    except Exception as e:
        print(f"ERROR: {e}")