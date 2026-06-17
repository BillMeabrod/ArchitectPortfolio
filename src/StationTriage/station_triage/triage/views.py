from django.shortcuts import render, get_object_or_404, redirect
from .models import ShipAssessment

def security_queue(request):
    ships = ShipAssessment.objects.filter(
        security_hazard_level__gt=0
    ).exclude(
        security_status=ShipAssessment.Status.RESOLVED
    ).order_by('-security_hazard_level')
    return render(request, 'triage/security_queue.html', {'ships': ships})

def security_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        ship.security_status = request.POST.get('security_status')
        ship.save()
        return redirect('security_queue')
    return render(request, 'triage/security_detail.html', {'ship': ship})

def medical_queue(request):
    ships = ShipAssessment.objects.filter(
        biohazard_level__gt=0
    ).exclude(
        medical_status=ShipAssessment.Status.RESOLVED
    ).order_by('-biohazard_level')
    return render(request, 'triage/medical_queue.html', {'ships': ships})

def medical_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        ship.medical_status = request.POST.get('medical_status')
        ship.save()
        return redirect('medical_queue')
    return render(request, 'triage/medical_detail.html', {'ship': ship})

def hazmat_queue(request):
    ships = ShipAssessment.objects.filter(
        chemical_hazard_level__gt=0
    ).exclude(
        hazmat_status=ShipAssessment.Status.RESOLVED
    ).order_by('-chemical_hazard_level')
    return render(request, 'triage/hazmat_queue.html', {'ships': ships})

def hazmat_detail(request, id):
    ship = get_object_or_404(ShipAssessment, id=id)
    if request.method == 'POST':
        ship.hazmat_status = request.POST.get('hazmat_status')
        ship.save()
        return redirect('hazmat_queue')
    return render(request, 'triage/hazmat_detail.html', {'ship': ship})