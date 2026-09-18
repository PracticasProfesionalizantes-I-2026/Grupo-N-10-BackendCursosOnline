namespace Lumen.Shared.Enums;

public enum UserRole { Alumno, Profesor, Administrador }
public enum CourseOperationalStatus { Activo, Pausado, Finalizado }
public enum CourseRevisionStatus { Borrador, EnRevision, CambiosSolicitados, Rechazado, Publicado }
public enum CourseReviewType { Creacion, Modificacion }
public enum CourseReviewDecision { Aprobada, CambiosSolicitados, Rechazada }
public enum EnrollmentStatus { Pendiente, Aprobada, Rechazada, Cancelada }
public enum ProgressStatus { NoIniciado, EnProgreso, Completado }

