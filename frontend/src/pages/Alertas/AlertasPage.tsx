import { useEffect, useMemo, useState } from 'react';
import {
  getAlertas,
  getAlertasResumen,
  gestionarAlerta,
  ignorarAlerta,
  recalcularAlertas,
} from '../../api/alertasApi';
import { getColaboradores } from '../../api/colaboradoresApi';
import { ApiError } from '../../api/httpClient';
import { AlertasFilters } from '../../components/alertas/AlertasFilters';
import { AlertasResumenCards } from '../../components/alertas/AlertasResumenCards';
import { AlertasTable } from '../../components/alertas/AlertasTable';
import { GestionarAlertaModal } from '../../components/alertas/GestionarAlertaModal';
import type {
  AlertaFilterValues,
  AlertaFilters as AlertaApiFilters,
  AlertaList,
  AlertaResumen,
  EstadoAlerta,
  TipoAlerta,
} from '../../types/alerta';
import type { ColaboradorList } from '../../types/colaborador';

const defaultFilters: AlertaFilterValues = {
  estadoAlerta: '',
  tipoAlerta: '',
  colaboradorId: '',
  desde: '',
  hasta: '',
  incluirInactivas: false,
};

type ModalState = {
  alerta: AlertaList;
  action: 'gestionar' | 'ignorar';
};

function toApiFilters(filters: AlertaFilterValues): AlertaApiFilters {
  return {
    estadoAlerta: filters.estadoAlerta ? (filters.estadoAlerta as EstadoAlerta) : undefined,
    tipoAlerta: filters.tipoAlerta ? (filters.tipoAlerta as TipoAlerta) : undefined,
    colaboradorId: filters.colaboradorId ? Number(filters.colaboradorId) : undefined,
    desde: filters.desde || undefined,
    hasta: filters.hasta || undefined,
    incluirInactivas: filters.incluirInactivas,
  };
}

function getErrorMessages(error: unknown) {
  if (error instanceof ApiError) {
    return error.errors.length > 0 ? error.errors : [error.message];
  }

  if (error instanceof Error) {
    return [error.message];
  }

  return ['No fue posible completar la operacion.'];
}

export function AlertasPage() {
  const [resumen, setResumen] = useState<AlertaResumen | null>(null);
  const [alertas, setAlertas] = useState<AlertaList[]>([]);
  const [colaboradores, setColaboradores] = useState<ColaboradorList[]>([]);
  const [filters, setFilters] = useState<AlertaFilterValues>(defaultFilters);
  const [isLoading, setIsLoading] = useState(true);
  const [isRecalculating, setIsRecalculating] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errors, setErrors] = useState<string[]>([]);
  const [modalErrors, setModalErrors] = useState<string[]>([]);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [modalState, setModalState] = useState<ModalState | null>(null);

  const actionables = useMemo(
    () =>
      alertas.filter(
        (alerta) => alerta.estadoAlerta === 'Pendiente' || alerta.estadoAlerta === 'Vencida',
      ).length,
    [alertas],
  );

  const loadStaticData = async () => {
    try {
      const colaboradoresResponse = await getColaboradores({ isActive: true });
      setColaboradores(colaboradoresResponse.data ?? []);
    } catch (error) {
      setErrors(getErrorMessages(error));
    }
  };

  const loadAlertas = async (currentFilters = filters) => {
    setIsLoading(true);
    setErrors([]);

    try {
      const [resumenResponse, alertasResponse] = await Promise.all([
        getAlertasResumen(),
        getAlertas(toApiFilters(currentFilters)),
      ]);

      setResumen(resumenResponse.data);
      setAlertas(alertasResponse.data ?? []);
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    document.title = 'Alertas | Portal RRHH FZ';
    void loadStaticData();
  }, []);

  useEffect(() => {
    const timeoutId = window.setTimeout(() => {
      void loadAlertas(filters);
    }, 250);

    return () => window.clearTimeout(timeoutId);
  }, [filters]);

  const handleRecalcular = async () => {
    setIsRecalculating(true);
    setErrors([]);
    setSuccessMessage(null);

    try {
      const response = await recalcularAlertas();
      const data = response.data;
      setSuccessMessage(
        data
          ? `Alertas recalculadas: ${data.alertasCreadas} nuevas, ${data.alertasActualizadasAVencidas} actualizadas a vencidas, ${data.totalAlertasActivas} activas.`
          : 'Alertas recalculadas correctamente.',
      );
      await loadAlertas(filters);
    } catch (error) {
      setErrors(getErrorMessages(error));
    } finally {
      setIsRecalculating(false);
    }
  };

  const handleModalSubmit = async (observacionGestion: string | null) => {
    if (!modalState) {
      return;
    }

    setIsSubmitting(true);
    setModalErrors([]);
    setSuccessMessage(null);

    try {
      if (modalState.action === 'gestionar') {
        await gestionarAlerta(modalState.alerta.alertaId, { observacionGestion });
        setSuccessMessage('Alerta gestionada correctamente.');
      } else {
        await ignorarAlerta(modalState.alerta.alertaId, { observacionGestion });
        setSuccessMessage('Alerta ignorada correctamente.');
      }

      setModalState(null);
      await loadAlertas(filters);
    } catch (error) {
      setModalErrors(getErrorMessages(error));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="alertas-page">
      <section className="page-heading">
        <div>
          <span className="eyebrow">Vencimientos</span>
          <h2>Alertas</h2>
        </div>
        <button
          className="primary-button"
          disabled={isRecalculating}
          type="button"
          onClick={handleRecalcular}
        >
          {isRecalculating ? 'Recalculando...' : 'Recalcular alertas'}
        </button>
      </section>

      {successMessage && <div className="success-message">{successMessage}</div>}
      {errors.length > 0 && (
        <div className="form-error-list">
          {errors.map((error) => (
            <span key={error}>{error}</span>
          ))}
        </div>
      )}

      <AlertasResumenCards resumen={resumen} />

      <section className="summary-strip alertas-summary-strip" aria-label="Acciones pendientes">
        <article>
          <span>Alertas accionables</span>
          <strong>{actionables}</strong>
        </article>
        <article>
          <span>Listado actual</span>
          <strong>{alertas.length}</strong>
        </article>
        <article>
          <span>Incluye inactivas</span>
          <strong>{filters.incluirInactivas ? 'Si' : 'No'}</strong>
        </article>
      </section>

      <AlertasFilters
        colaboradores={colaboradores}
        filters={filters}
        onChange={(nextFilters) => {
          setFilters(nextFilters);
          setSuccessMessage(null);
        }}
        onClear={() => setFilters(defaultFilters)}
      />

      <AlertasTable
        alertas={alertas}
        isLoading={isLoading}
        onGestionar={(alerta) => {
          setModalErrors([]);
          setModalState({ alerta, action: 'gestionar' });
        }}
        onIgnorar={(alerta) => {
          setModalErrors([]);
          setModalState({ alerta, action: 'ignorar' });
        }}
      />

      {modalState && (
        <GestionarAlertaModal
          action={modalState.action}
          alerta={modalState.alerta}
          apiErrors={modalErrors}
          isSubmitting={isSubmitting}
          onClose={() => setModalState(null)}
          onSubmit={handleModalSubmit}
        />
      )}
    </div>
  );
}
