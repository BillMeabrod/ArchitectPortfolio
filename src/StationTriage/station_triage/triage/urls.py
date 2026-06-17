from django.urls import path
from . import views

urlpatterns = [
    path('security/', views.security_queue, name='security_queue'),
    path('security/<int:id>/', views.security_detail, name='security_detail'),
    path('medical/', views.medical_queue, name='medical_queue'),
    path('medical/<int:id>/', views.medical_detail, name='medical_detail'),
    path('hazmat/', views.hazmat_queue, name='hazmat_queue'),
    path('hazmat/<int:id>/', views.hazmat_detail, name='hazmat_detail'),
]