import json
from django.http import JsonResponse
from django.views.decorators.csrf import csrf_exempt
from django.shortcuts import get_object_or_404
from .models import ShipAssessment

def security_queue(request):
    ships = ShipAssessment.objects.filter(
        security_hazard_level__gt=0
    ).exclude(
        security_status=ShipAssessment.Status.RESOLVED
    ).order_by('-security_hazard_level')
    data = [
        {
            'id': s.id,
            'ship_name': s.ship_name,
            'callsign': s.callsign,
            'captain_name': s.captain_name,
            'security_hazard_level': s.security_hazard_level,
            'security_status': s.security_status,
        }
        for s in ships
    ]
    return JsonResponse(data, safe=False)

@csrf_exempt
def security_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        body = json.loads(request.body)
        ship.security_status = body.get('security_status')
        ship.save()
        return JsonResponse({'ok': True})
    data = {
        'id': ship.id,
        'ship_name': ship.ship_name,
        'callsign': ship.callsign,
        'captain_name': ship.captain_name,
        'security_hazard_level': ship.security_hazard_level,
        'security_status': ship.security_status,
        'recommendation': ship.recommendation,
        'cargo_items': ship.cargo_items,
        'passengers': ship.passengers,
    }
    return JsonResponse(data)

def medical_queue(request):
    ships = ShipAssessment.objects.filter(
        biohazard_level__gt=0
    ).exclude(
        medical_status=ShipAssessment.Status.RESOLVED
    ).order_by('-biohazard_level')
    data = [
        {
            'id': s.id,
            'ship_name': s.ship_name,
            'callsign': s.callsign,
            'captain_name': s.captain_name,
            'biohazard_level': s.biohazard_level,
            'medical_status': s.medical_status,
        }
        for s in ships
    ]
    return JsonResponse(data, safe=False)

@csrf_exempt
def medical_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        body = json.loads(request.body)
        ship.medical_status = body.get('medical_status')
        ship.save()
        return JsonResponse({'ok': True})
    data = {
        'id': ship.id,
        'ship_name': ship.ship_name,
        'callsign': ship.callsign,
        'captain_name': ship.captain_name,
        'biohazard_level': ship.biohazard_level,
        'medical_status': ship.medical_status,
        'recommendation': ship.recommendation,
        'cargo_items': ship.cargo_items,
        'passengers': ship.passengers,
    }
    return JsonResponse(data)

def hazmat_queue(request):
    ships = ShipAssessment.objects.filter(
        chemical_hazard_level__gt=0
    ).exclude(
        hazmat_status=ShipAssessment.Status.RESOLVED
    ).order_by('-chemical_hazard_level')
    data = [
        {
            'id': s.id,
            'ship_name': s.ship_name,
            'callsign': s.callsign,
            'captain_name': s.captain_name,
            'chemical_hazard_level': s.chemical_hazard_level,
            'hazmat_status': s.hazmat_status,
        }
        for s in ships
    ]
    return JsonResponse(data, safe=False)

@csrf_exempt
def hazmat_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        body = json.loads(request.body)
        ship.hazmat_status = body.get('hazmat_status')
        ship.save()
        return JsonResponse({'ok': True})
    data = {
        'id': ship.id,
        'ship_name': ship.ship_name,
        'callsign': ship.callsign,
        'captain_name': ship.captain_name,
        'chemical_hazard_level': ship.chemical_hazard_level,
        'hazmat_status': ship.hazmat_status,
        'recommendation': ship.recommendation,
        'cargo_items': ship.cargo_items,
        'passengers': ship.passengers,
    }
    return JsonResponse(data)