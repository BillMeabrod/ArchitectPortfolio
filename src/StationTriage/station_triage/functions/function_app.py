import os
import json
import logging
import psycopg2
import azure.functions as func

app = func.FunctionApp()

@app.queue_trigger(arg_name="msg", queue_name="risk-assessment-queue", connection="AzureWebJobsStorage")
def process_risk_assessment(msg: func.QueueMessage):
    conn = None
    try:
        body = json.loads(msg.get_body().decode('utf-8'))
        manifest = body['Manifest']
        assessment = body['Assessment']

        conn = psycopg2.connect(os.environ['DATABASE_URL'])
        cur = conn.cursor()
        cur.execute(
            """INSERT INTO triage_shipassessment
               (ship_name, callsign, captain_name, cargo_items, passengers,
                biohazard_level, chemical_hazard_level, security_hazard_level,
                recommendation, security_status, medical_status, hazmat_status,
                inappropriate_content, received_at)
               VALUES (%s, %s, %s, %s, %s, %s, %s, %s, %s, 'NEW', 'NEW', 'NEW', %s, NOW())""",
            (manifest['ShipName'], manifest['Callsign'], manifest['CaptainName'],
             json.dumps(manifest['CargoItems']), json.dumps(manifest['Passengers']),
             assessment['BiohazardLevel'], assessment['ChemicalHazardLevel'],
             assessment['SecurityHazardLevel'], assessment['Recommendation'],
             assessment['InappropriateContent'])
        )
        conn.commit()
        cur.close()
        logging.info(f"Created ShipAssessment for {manifest['Callsign']}")
    except Exception:
        logging.exception("Failed to process risk assessment message")
    finally:
        if conn is not None:
            conn.close()